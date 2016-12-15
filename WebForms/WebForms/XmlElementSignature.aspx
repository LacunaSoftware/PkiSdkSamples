<%@ Page Title="Xml Element Signature" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="XmlElementSignature.aspx.cs" Inherits="WebForms.XmlElementSignature" %>

<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">

	<h2>Xml Element Signature</h2>	

	<asp:ValidationSummary runat="server" CssClass="text-danger" />

	<asp:Panel ID="signatureControlsPanel" runat="server">
		<p>You are signing the <i>infNFe</i> node of <a href='/Content/SampleNFe.xml'>this sample XML</a>.</p>

		<%-- Render a select (combo box) to list the user's certificates. For now it will be empty, we'll populate it later on (see javascript below). --%>
		<div class="form-group">
			<label for="certificateSelect">Selecione um certificado</label>
			<select id="certificateSelect" class="form-control"></select>
		</div>

		<%--
			Action buttons. Notice that both buttons have a OnClientClick attribute, which calls the
			client-side javascript functions "sign" and "refresh" below. Both functions return false,
			which prevents the postback.
		--%>
		<asp:Button ID="SignButton" runat="server" class="btn btn-primary" Text="Assinar" OnClientClick="return sign();" />
		<asp:Button ID="RefreshButton" runat="server" class="btn btn-default" Text="Recarregar certificados" OnClientClick="return refresh();" />
	</asp:Panel>

	<%--
		Hidden fields used to store state between the signature steps
	--%>
	<asp:HiddenField runat="server" ID="TransferDataField" />

	<%--
		Hidden fields used to pass data from the "code behind" to the "signature form" javascript (see below) and vice-versa 
	--%>
	<asp:HiddenField runat="server" ID="CertThumbField" />
	<asp:HiddenField runat="server" ID="CertContentField" />
	<asp:HiddenField runat="server" ID="ToSignHashField" />
	<asp:HiddenField runat="server" ID="DigestAlgorithmField" />
	<asp:HiddenField runat="server" ID="SignatureField" />

	<%--
		Hidden buttons whose click event is fired by the "signature form" javascript upon completion of
		each step in the signature process. Notice that we cannot use Visible="False" otherwise ASP.NET
		will omit the button altogether from the rendered page, making it impossible to programatically
		"click" it.
	--%>
	<asp:Button ID="SubmitCertificateButton" runat="server" OnClick="SubmitCertificateButton_Click" Style="display: none;" />
	<asp:Button ID="SubmitSignatureButton" runat="server" OnClick="SubmitSignatureButton_Click" Style="display: none;" />

	<asp:HyperLink ID="TryAgainButton" runat="server" href="XmlElementSignature" class="btn btn-default" Text="Try Again" style="display: none;" />

	<script>
		<%--
			Once the page is loaded, we'll call the init() function on the signature-form.js file passing references to
			our page's elements and hidden fields
		--%>
		$(function () {
			signatureForm.init({
				<%-- References to the certificate combo box and the div surrounding the combo box and the signature buttons --%>
				certificateSelect: $('#certificateSelect'),
				<%-- Hidden buttons to transfer the execution back to the server-side code behind --%>
				submitCertificateButton: $('#<%= SubmitCertificateButton.ClientID %>'),
				submitSignatureButton: $('#<%= SubmitSignatureButton.ClientID %>'),
				<%-- Hidden fields to pass data to and from the server-side code-behind --%>
				certThumbField: $('#<%= CertThumbField.ClientID %>'),
				certContentField: $('#<%= CertContentField.ClientID %>'),
				toSignHashField: $('#<%= ToSignHashField.ClientID %>'),
				digestAlgorithmField: $('#<%= DigestAlgorithmField.ClientID %>'),
				signatureField: $('#<%= SignatureField.ClientID %>'),
				<%-- Try Again Button for the case the complete action not works or the signature process is canceled --%>
				tryAgainButton: $('#<%= TryAgainButton.ClientID %>')
			});
		});
		<%-- Client-side function called when the user clicks the "Sign In" button --%>
		function sign() {
			signatureForm.startSignature();
			return false; // prevent postback
		}
		<%-- Client-side function called when the user clicks the "Refresh" button --%>
		function refresh() {
			signatureForm.refresh();
			return false; // prevent postback
		}
	</script>

</asp:Content>
