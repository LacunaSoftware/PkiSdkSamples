<%@ Page Title="Certificate Authentication" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Authentication.aspx.cs" Inherits="WebForms.Authentication" %>

<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">

	<asp:HiddenField runat="server" ID="CertificateField" />
	<asp:HiddenField runat="server" ID="NonceField" />
	<asp:HiddenField runat="server" ID="DigestAlgorithmField" />
	<asp:HiddenField runat="server" ID="SignatureField" />
	
	<asp:ValidationSummary runat="server" CssClass="text-danger" />

	<h2>Certificate Authentication</h2>

	<div class="form-group">
		<label for="certificateSelect">Select a certificate</label>
		<select id="certificateSelect" class="form-control"></select>
	</div>

	<asp:Button ID="SignInButton" runat="server" class="btn btn-primary" Text="Sign In" OnClientClick="return signIn();" />
	<asp:Button ID="RefreshButton" runat="server" class="btn btn-default" Text="Refresh Certificates" OnClientClick="return refresh();" />
	<asp:Button ID="SubmitButton" runat="server" OnClick="SubmitButton_Click" Style="display: none;" />

	<script>
		<%--
			Once the page is loaded, we'll call the init() function on the signature-form.js file passing
			references to our page's elements and hidden fields.
		--%>
		$(function () {
			authenticationForm.pageLoad({
				<%--
					References to the certificate combo box and the div surrounding the combo box and the
					signature buttons.
				--%>
				certificateSelect: $('#certificateSelect'),
				<%-- Hidden buttons to transfer the execution back to the server-side code behind. --%>
				submitButton: $('#<%= SubmitButton.ClientID %>'),
				<%-- Hidden fields to pass data to and from the server-side code-behind. --%>
				certificateField: $('#<%= CertificateField.ClientID %>'),
				nonceField: $('#<%= NonceField.ClientID %>'),
				digestAlgorithmField: $('#<%= DigestAlgorithmField.ClientID %>'),
				signatureField: $('#<%= SignatureField.ClientID %>')
			});
		});
		<%-- Client-side function called when the user clicks the "Sign In" button. --%>
		function signIn() {
			authenticationForm.signIn();
			return false; // Prevent postback.
		}
		<%-- Client-side function called when the user clicks the "Refresh" button. --%>
		function refresh() {
			authenticationForm.refresh();
			return false; // Prevent postback.
		}
	</script>

</asp:Content>
