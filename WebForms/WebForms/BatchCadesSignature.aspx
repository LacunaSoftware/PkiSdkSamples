<%@ Page Title="Batch Cades Signature" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="BatchCadesSignature.aspx.cs" Inherits="WebForms.BatchCadesSignature" %>

<%--
	This page uses the Javascript module "batch signature form" (see file batch-signature-form.js). That
	javascript is only a sample, you are encouraged to alter it to meet your application's needs.
--%>

<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">

	<h2>Batch signature</h2>

	<%--
		UpdatePanel used to refresh only this part of the page. This is needed because if we did a complete
		postback of the page, the Web PKI component would ask for user authorization to sign each document in
		the batch.
	--%>
	<asp:UpdatePanel runat="server">
		<ContentTemplate>

			<%--
				ListView to show each batch document and either the download link for the signed version (if
				successful) or an error message (if failed).
			--%>
			<asp:ListView ID="DocumentsListView" runat="server">
				<LayoutTemplate>
					<ul>
						<asp:PlaceHolder ID="itemPlaceholder" runat="server" />
					</ul>
				</LayoutTemplate>
				<ItemTemplate>
					<li>Document
						<asp:Label runat="server" Text='<%# Eval("Id") %>' />
						<asp:Label runat="server" Visible='<%# Eval("Error") != null %>' Text='<%# Eval("Error") %>' CssClass="text-danger" />
						<asp:HyperLink runat="server" Visible='<%# Eval("DownloadLink") != null %>' NavigateUrl='<%# Eval("DownloadLink") %>' Text="download" />
					</li>
				</ItemTemplate>
			</asp:ListView>

			<%--
				Surrounding panel containing the certificate select (combo box) and buttons, which is hidden
				by the code-behind after the batch starts.
			--%>
			<asp:Panel ID="SignatureControlsPanel" runat="server">

				<%-- 
					Render a select (combo box) to list the user's certificates. For now it will be empty,
					we'll populate it later on (see batch-signature-form.js).
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

			<%-- Hidden fields used to pass data from the code-behind to the javascript and vice-versa. --%>
			<asp:HiddenField runat="server" ID="CertificateField" />
			<asp:HiddenField runat="server" ID="ToSignHashField" />
			<asp:HiddenField runat="server" ID="DigestAlgorithmField" />
			<asp:HiddenField runat="server" ID="SignatureField" />

			<%--
				Hidden fields used by the code-behind to save state between signature steps. These could be
				alternatively stored on server-side session, since we don't need their values on the
				javascript.
			--%>
			<asp:HiddenField runat="server" ID="ToSignBytesField" />
			<asp:HiddenField runat="server" ID="DocumentIdsField" />
			<asp:HiddenField runat="server" ID="DocumentIndexField" />
			<asp:HiddenField runat="server" ID="TransferDataFileIdField" />

			<%--
				Hidden buttons whose click event is fired programmatically by the javascript upon completion
				of each step in the batch. Notice that	we cannot use Visible="False" otherwise ASP.NET will
				omit the button altogether from the rendered page, making it impossible to programatically
				"click" it.
			--%>
			<asp:Button ID="SubmitCertificateButton" runat="server" OnClick="SubmitCertificateButton_Click" Style="display: none;" />
			<asp:Button ID="SubmitSignatureButton" runat="server" OnClick="SubmitSignatureButton_Click" Style="display: none;" />

		</ContentTemplate>
	</asp:UpdatePanel>

	<script>

		<%--
			Set the number of documents in the batch on the "batch signature form" javascript module. This is
			needed in order to request user permissions to make N signatures (the Web PKI component requires
			us to inform the number of signatures that will be performed on the batch).
		--%>
		batchSignatureForm.setDocumentCount(<%= DocumentIds.Count %>);

		<%--
			The function below is called by ASP.NET's javascripts when the page is loaded and also when the
			UpdatePanel above changes. We'll call the pageLoaded() function on the batch-signature-form.js
			passing references to our page's elements and hidden fields.
		--%>
		function pageLoad() {

			batchSignatureForm.pageLoad({

				<%-- Reference to the certificate combo box. --%>
				certificateSelect: $('#certificateSelect'),

				<%-- Hidden buttons to transfer the execution back to the code-behind. --%>
				submitCertificateButton: $('#<%= SubmitCertificateButton.ClientID %>'),
				submitSignatureButton: $('#<%= SubmitSignatureButton.ClientID %>'),

				<%-- Hidden fields to pass data to and from the code-behind. --%>
				certificateField: $('#<%= CertificateField.ClientID %>'),
				toSignHashField: $('#<%= ToSignHashField.ClientID %>'),
				digestAlgorithmField: $('#<%= DigestAlgorithmField.ClientID %>'),
				signatureField: $('#<%= SignatureField.ClientID %>')

			});
		}

		<%-- Client-side function called when the user clicks the "Sign" button. --%>
		function sign() {
			batchSignatureForm.start();
			return false; // Prevent postback.
		}

		<%-- Client-side function called when the user clicks the "Refresh" button. --%>
		function refresh() {
			batchSignatureForm.refresh();
			return false; // Prevent postback.
		}

	</script>

</asp:Content>
