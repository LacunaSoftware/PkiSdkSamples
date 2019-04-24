<%@ Page Title="Batch Pades Signature" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="BatchPadesSignatureWebAPI.aspx.cs" Inherits="WebForms.BatchPadesSignatureWebAPI" %>

<%--
This page uses the Javascript module "batch signature form" (see file Scripts/Apps/batch-signature-form.js).
That javascript is only a sample, you are encouraged to alter it to meet your application's needs.
--%>

<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">
	<h2>Batch signature</h2>
	<div id="messagesPanel"></div>
	<asp:ListView ID="DocumentsListView" runat="server">
		<LayoutTemplate>
			<ul id="docList">
				<asp:PlaceHolder ID="itemPlaceholder" runat="server" />
			</ul>
		</LayoutTemplate>
		<ItemTemplate>
			<li>Document
				<asp:Label runat="server" Text='<%# Eval("Id") %>' />
				<span id='error<%# Eval("Id")%>' style="display: none"></span>
				<a id='download<%# Eval("Id")%>' style="display: none"></a>
			</li>
		</ItemTemplate>
	</asp:ListView>

	<%--
			Surrounding panel containing the certificate select (combo box) and buttons, which is hidden by
            the code-behind after the batch starts.
	--%>
	<asp:Panel ID="SignatureControlsPanel" runat="server">

		<%-- 
                Render a select (combo box) to list the user's certificates. For now it will be empty, we'll
                populate it later on (see batch-signature-form.js). 
		--%>
		<div class="form-group">
			<label for="certificateSelect">Choose a certificate</label>
			<select id="certificateSelect" class="form-control"></select>
		</div>

		<%--
				Action buttons. Notice that both buttons have a OnClientClick attribute, which calls the
				client-side javascript functions "sign" and "refresh" below. Both functions return false,
				which prevents the postback.
		--%>
		<asp:Button ID="SignButton" runat="server" class="btn btn-primary" Text="Sign Batch" OnClientClick="return sign();" />
		<asp:Button ID="RefreshButton" runat="server" class="btn btn-default" Text="Refresh Certificates" OnClientClick="return refresh();" />

	</asp:Panel>

	<%-- Hidden fields used to pass data from the code-behind to the javascript and vice-versa.	--%>
	<asp:HiddenField runat="server" ID="CertificateField" />
	<asp:HiddenField runat="server" ID="ToSignHashField" />
	<asp:HiddenField runat="server" ID="DigestAlgorithmField" />
	<asp:HiddenField runat="server" ID="SignatureField" />

	<%--
			Hidden fields used by the code-behind to save state between signature steps. These could be
            alternatively stored on server-side session, since we don't need their values on the javascript.
	--%>
	<asp:HiddenField runat="server" ID="DocumentIdsField" />
	<asp:HiddenField runat="server" ID="DocumentIndexField" />
	<asp:HiddenField runat="server" ID="TransferDataFileIdField" />



	<script>

		<%--
		Set the number of documents in the batch on the "batch signature form" javascript module. This is
        needed in order to request user permissions to make N signatures (the Web PKI component requires us
        to inform the number of signatures that will be performed on the batch).
		--%>
		var docCount = <%= DocumentIds.Count %>;
		var formElements = {
			certificateSelect: $('#certificateSelect')
		};
		var selectedCertThumbprint = null;
		var selectedCertContent = null;
		var pki = null;

		var startQueue = null;
		var performQueue = null;
		var completeQueue = null;


		function pageLoad() {
			$.blockUI({ message: 'Initializing ...' });
			// Create an instance of the LacunaWebPKI object.
			pki = new LacunaWebPKI();
			// Call the init() method on the LacunaWebPKI object, passing a callback for when the component is
			// ready to be used and another to be called when an error occurrs on any of the subsequent
			// operations. For more information, see:
			// https://docs.lacunasoftware.com/en-us/articles/web-pki/get-started.html#coding-the-first-lines
			// http://webpki.lacunasoftware.com/Help/classes/LacunaWebPKI.html#method_init
			pki.init({
				ready: loadCertificates,    // As soon as the component is ready we'll load the certificates.
				defaultError: onWebPkiError // Generic error callback defined below.
			});
		}

		function loadCertificates() {

			// Call the listCertificates() method to list the user's certificates. For more information see:
			// http://webpki.lacunasoftware.com/Help/classes/LacunaWebPKI.html#method_listCertificates
			pki.listCertificates({
				// The ID of the <select> element to be populated with the certificates.
				selectId: 'certificateSelect',
				// Function that will be called to get the text that should be displayed for each option.
				selectOptionFormatter: function (cert) {
					var s = cert.subjectName + ' (issued by ' + cert.issuerName + ')';
					if (new Date() > cert.validityEnd) {
						s = '[EXPIRED] ' + s;
					}
					return s;
				}

			}).success(function () {
				// Once the certificates have been listed, unblock the UI.
				$.unblockUI();
			});
		}


		<%-- Client-side function called when the user clicks the "Sign" button. --%>
		function sign() {
			$.blockUI();

			// Get the thumbprint of the selected certificate and store it in a global variable (we'll need it later)
			selectedCertThumbprint = formElements.certificateSelect.val();

			pki.readCertificate(selectedCertThumbprint).success(function (certEncoded) {
				// Store the certificate content
				selectedCertContent = certEncoded;
				// Call Web PKI to preauthorize the signatures, so that the user only sees one confirmation dialog
				pki.preauthorizeSignatures({
					certificateThumbprint: selectedCertThumbprint,
					signatureCount: docCount // number of signatures to be authorized by the user
				}).success(startBatch); // callback to be called if the user authorizes the signatures
			});
			return false; // Prevent postback.
		}


		function startBatch() {
			startQueue = new Queue();
			performQueue = new Queue();
			completeQueue = new Queue();

			// Add all documents to the first ("start") queue
			for (var i = 0; i < docCount; i++) {
				startQueue.add({ index: i, docId: i });
			}

			startQueue.process(startSignature, { threads: 2, output: performQueue });
			performQueue.process(performSignature, { threads: 2, output: completeQueue });
			completeQueue.process(completeSignature, { threads: 2, completed: onBatchCompleted }); // onBatchCompleted is a callback for when the last queue is completely processed
		}

		function startSignature(step, done) {
			// Call the server asynchronously to start the signature
			$.ajax({
				url: 'Api/Sign/Start',
				method: 'POST',
				data: {
					id: step.docId,
					certContentBase64: selectedCertContent
				},
				dataType: 'json',
				success: function (response) {
					// Add the parameters to the document information (we'll need it in the second and third steps)
					step.transferDataFileId = response.transferDataFileId;
					step.toSignHash = response.toSignHashBase64;
					step.digestAlgorithmOid = response.digestAlgorithmOid;
					// Call the "done" callback signalling we're done with the document
					done(step);
				},
				error: function (jqXHR, textStatus, errorThrown) {
					// Print on console the error message
					console.log('Document ' + step.docId, jqXHR.responseJSON);
					// Render error
					renderFail(step, errorThrown || textStatus);
					// Call the "done" callback with no argument, signalling the document should not go to the next queue
					done();
				}
			});
		}

		// -------------------------------------------------------------------------------------------------
		// Function that performs the second step described above for each document, which is the call to
		// Web PKI's signHash function using the "to-sign-hash" and the digest algorithm acquired on the 
		// first step.
		//
		// This function is called by the Queue.process function, taking documents from the "perform" queue.
		// Once we're done, we'll call the "done" callback passing the document, and the Queue.process
		// function will place the document on the "complete" queue to await processing.
		// -------------------------------------------------------------------------------------------------
		function performSignature(step, done) {
			// Call signHash() on the Web PKI component passing the "to-sign-hash", the digest algorithm and the certificate selected by the user.
			pki.signHash({
				thumbprint: selectedCertThumbprint,
				hash: step.toSignHash,
				digestAlgorithm: step.digestAlgorithmOid
			}).success(function (signature) {
				// Call the "done" callback signalling we're done with the document
				step.signature = signature;
				done(step);
			}).error(function (error) {
				// Render error
				renderFail(step, error);
				// Call the "done" callback with no argument, signalling the document should not go to the next queue
				done();
			});
		}

		// -------------------------------------------------------------------------------------------------
		// Function that performs the third step described above for each document, which is the call to the
		// action Api/BatchSignature/Complete in order to complete the signature.
		//
		// This function is called by the Queue.process function, taking documents from the "complete" queue.
		// Once we're done, we'll call the "done" callback passing the document. Once all documents are
		// processed, the Queue.process will call the "onBatchCompleted" function.
		// -------------------------------------------------------------------------------------------------
		function completeSignature(step, done) {
			// Call the server asynchronously to notify that the signature has been performed
			$.ajax({
				url: 'Api/Sign/Complete',
				method: 'POST',
				data: {
					signatureBase64: step.signature,
					transferDataFileId: step.transferDataFileId
				},
				dataType: 'json',
				success: function (response) {
					step.signedFileId = response.signedFileId;
					// Render success
					renderSuccess(step);
					// Call the "done" callback signalling we're done with the document
					done(step);
				},
				error: function (jqXHR, textStatus, errorThrown) {
					// Print on console the error message
					console.log('Document ' + step.docId, jqXHR.responseJSON);
					// Render error
					renderFail(step, errorThrown || textStatus);
					// Call the "done" callback with no argument, signalling the document should not go to the next queue
					done();
				}
			});
		}

		// -------------------------------------------------------------------------------------------------
		// Function called once the batch is completed.
		// -------------------------------------------------------------------------------------------------
		function onBatchCompleted() {
			// Notify the user and unblock the UI
			addAlert('info', 'Batch processing completed');
			// Prevent user from clicking "sign batch" again (our logic isn't prepared for that)
			$('#SignButton').prop('disabled', true);
			// Unblock the UI
			$.unblockUI();
		}

		// -------------------------------------------------------------------------------------------------
		// Function that renders a documument as completed successfully
		// -------------------------------------------------------------------------------------------------
		function renderSuccess(step) {
			var docLi = $('#docList li').eq(step.index);
			docLi.append(
				document.createTextNode(' ')
			).append(
				$('<span />').addClass('glyphicon glyphicon-arrow-right')
			).append(
				document.createTextNode(' ')
			).append(
				$('<a />').text(step.signedFileId.replace('_', '.')).attr('href', '/Download?File=' + step.signedFileId)
			);
		}

		// -------------------------------------------------------------------------------------------------
		// Function that renders a documument as failed
		// -------------------------------------------------------------------------------------------------
		function renderFail(step, error) {
			addAlert('danger', 'An error has occurred while signing Document ' + step.docId + ': ' + error);
			var docLi = $('#docList li').eq(step.index);
			docLi.append(
				document.createTextNode(' ')
			).append(
				$('<span />').addClass('glyphicon glyphicon-remove')
			);
		}

		function addAlert(type, message) {
			$('#messagesPanel').append(
				'<div class="alert alert-' + type + ' alert-dismissible">' +
				'<button type="button" class="close" data-dismiss="alert" aria-label="Close"><span aria-hidden="true">&times;</span></button>' +
				'<span>' + message + '</span>' +
				'</div>');
		}


		<%-- Client-side function called when the user clicks the "Refresh" button. --%>
		function refresh() {
			batchSignatureForm.refresh();
			return false; // Prevent postback.
		}

		function onWebPkiError(message, error, origin) {
			// Unblock the UI.
			$.unblockUI();
			// Log the error to the browser console (for debugging purposes).
			if (console) {
				console.log('An error has occurred on the signature browser component: ' + message, error);
			}
			// Show the message to the user. You might want to substitute the alert below with a more
			// user-friendly UI component to show the error.
			alert(message);
		}

		(function () {
			window.Queue = function () {
				this.items = [];
				this.writerCount = 0;
				this.readerCount = 0;
			};
			window.Queue.prototype.add = function (e) {
				this.items.push(e);
			};
			window.Queue.prototype.addRange = function (array) {
				for (var i = 0; i < array.length; i++) {
					this.add(array[i]);
				}
			};
			var _process = function (inQueue, processor, outQueue, endCallback) {
				var obj = inQueue.items.shift();
				if (obj !== undefined) {
					processor(obj, function (result) {
						if (result != null && outQueue != null) {
							outQueue.add(result);
						}
						_process(inQueue, processor, outQueue, endCallback);
					});
				} else if (inQueue.writerCount > 0) {
					setTimeout(function () {
						_process(inQueue, processor, outQueue, endCallback);
					}, 200);
				} else {
					--inQueue.readerCount;
					if (outQueue != null) {
						--outQueue.writerCount;
					}
					if (inQueue.readerCount == 0 && endCallback) {
						endCallback();
					}
				}
			};
			window.Queue.prototype.process = function (processor, options) {
				var threads = options.threads || 1;
				this.readerCount = threads;
				if (options.output) {
					options.output.writerCount = threads;
				}
				for (var i = 0; i < threads; i++) {
					_process(this, processor, options.output, options.completed);
				}
			};
		})();



	</script>

</asp:Content>
