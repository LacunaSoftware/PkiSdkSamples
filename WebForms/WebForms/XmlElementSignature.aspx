<%@ Page Title="Xml Element Signature" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="XmlElementSignature.aspx.cs" Inherits="WebForms.XmlElementSignature" %>

<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">

	<h2>Xml Element Signature</h2>

	<%--
		UpdatePanel used to refresh only this part of the page. This is used to send the selected
		certificate's encoding to the code-behind and receiving back the parameters for the signature
		algorithm computation.
	--%>
	<asp:UpdatePanel runat="server">
		<ContentTemplate>

			<asp:ValidationSummary runat="server" CssClass="text-danger" />

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
			<asp:HiddenField runat="server" ID="TransferDataField" />

			<%--
				Hidden button whose click event is fired by the "signature form" javascript upon acquiring
				the selected certificate's encoding. Notice that we cannot use Visible="False" otherwise
				ASP.NET will omit the button altogether from the rendered page, making it impossible to
				programatically "click" it. Notice also that this button is inside the UpdatePanel,
				triggerring only a partial postback.
			--%>
			<asp:Button ID="SubmitCertificateButton" runat="server" OnClick="SubmitCertificateButton_Click" Style="display: none;" />

		</ContentTemplate>
	</asp:UpdatePanel>

	<label>File to sign</label>
	<p>You are signing the <i>infNFe</i> node of <a href='/Content/SampleNFe.xml'>this sample XML</a>.</p>

	<%-- 
		Render a select (combo box) to list the user's certificates. For now it will be empty, we'll populate
		it later on (see signature-form.js).
	--%>
	<div class="form-group">
		<label for="certificateSelect">Selecione um certificado</label>
		<select id="certificateSelect" class="form-control"></select>
	</div>

	<%--
		Action buttons. Notice that both buttons have a OnClientClick attribute, which calls the client-side
		javascript functions "sign" and "refresh" below. Both functions return false, which prevents the
		postback.
	--%>
	<asp:Button ID="SignButton" runat="server" class="btn btn-primary" Text="Sign File" OnClientClick="return sign();" />
	<asp:Button ID="RefreshButton" runat="server" class="btn btn-default" Text="Refresh Certificates" OnClientClick="return refresh();" />

	<%--
		Hidden button whose click event is fired by the "signature form" javascript upon completion of the
		signature of the "to sign hash". Notice that we cannot use Visible="False" otherwise ASP.NET will omit
		the button altogether from the rendered page, making it impossible to programatically "click" it.
		Notice also that this button is out of the UpdatePanel, triggering a complete postback.
	--%>
	<asp:Button ID="SubmitSignatureButton" runat="server" OnClick="SubmitSignatureButton_Click" Style="display: none;" />

	<script>

		<%--
			The function below is called by ASP.NET's javascripts when the page is loaded and also when the
			UpdatePanel above changes. We'll call the pageLoaded() function on the batch-signature-form.js
			passing references to our page's elements and hidden fields.
		--%>
		function pageLoad() {
			signatureForm.pageLoad({

				<%-- 
					References to the certificate combo box and the div surrounding the combo box and the
					signature buttons. 
				--%>
				certificateSelect: $('#certificateSelect'),

				<%-- Hidden buttons to transfer the execution back to the server-side code behind. --%>
				submitCertificateButton: $('#<%= SubmitCertificateButton.ClientID %>'),
				submitSignatureButton: $('#<%= SubmitSignatureButton.ClientID %>'),

				<%-- Hidden fields to pass data to and from the server-side code-behind. --%>
				certificateField: $('#<%= CertificateField.ClientID %>'),
				toSignHashField: $('#<%= ToSignHashField.ClientID %>'),
				digestAlgorithmField: $('#<%= DigestAlgorithmField.ClientID %>'),
				signatureField: $('#<%= SignatureField.ClientID %>')

			});
		}

		<%-- Client-side function called when the user clicks the "Sign In" button. --%>
		function sign() {
			signatureForm.startSignature();
			return false; // Prevent postback.
		}

		<%-- Client-side function called when the user clicks the "Refresh" button. --%>
		function refresh() {
			signatureForm.refresh();
			return false; // Prevent postback.
		}
	</script>

</asp:Content>
