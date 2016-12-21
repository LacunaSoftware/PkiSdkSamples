<%@ Page Title="Batch Signature" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="BatchSignature.aspx.cs" Inherits="WebForms.BatchSignature" %>

<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">

	<h2>Batch signature</h2>

	<asp:HiddenField runat="server" ID="DocumentIdsField" />

	<asp:UpdatePanel runat="server">
		<ContentTemplate>

			<asp:ListView ID="DocumentsListView" runat="server">
				<LayoutTemplate>
					<ul>
						<asp:PlaceHolder ID="itemPlaceholder" runat="server" />
					</ul>
				</LayoutTemplate>
				<ItemTemplate>
					<li>
						Document <asp:Label runat="server" Text='<%# Eval("Id") %>' />
						<asp:Label runat="server" Visible='<%# Eval("Error") != null %>' Text='<%# Eval("Error") %>' CssClass="text-danger" />
						<asp:HyperLink runat="server" Visible='<%# Eval("DownloadLink") != null %>' NavigateUrl='<%# Eval("DownloadLink") %>' Text="download" />
					</li>
				</ItemTemplate>
			</asp:ListView>

			<asp:Panel ID="SignatureControlsPanel" runat="server">
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

			<asp:HiddenField runat="server" ID="DocumentIndexField" />
			<asp:HiddenField runat="server" ID="TransferDataFileId" />

			<asp:HiddenField runat="server" ID="CertThumbField" />
			<asp:HiddenField runat="server" ID="CertContentField" />
			<asp:HiddenField runat="server" ID="ToSignHashField" />
			<asp:HiddenField runat="server" ID="DigestAlgorithmField" />
			<asp:HiddenField runat="server" ID="SignatureField" />

			<asp:Button ID="SubmitCertificateButton" runat="server" OnClick="SubmitCertificateButton_Click" Style="display: none;" />
			<asp:Button ID="SubmitSignatureButton" runat="server" OnClick="SubmitSignatureButton_Click" Style="display: none;" />

		</ContentTemplate>
	</asp:UpdatePanel>

	<script>
		batchSignatureForm.setDocumentCount(<%= DocumentIds.Count %>);
		<%--
			Once the page is loaded, we'll call the init() function on the signature-form.js file passing references to
			our page's elements and hidden fields
		--%>
		function pageLoad() {
			batchSignatureForm.pageLoad({
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
				signatureField: $('#<%= SignatureField.ClientID %>')
			});
		}
		<%-- Client-side function called when the user clicks the "Sign In" button --%>
		function sign() {
			batchSignatureForm.start();
			return false; // prevent postback
		}
		<%-- Client-side function called when the user clicks the "Refresh" button --%>
		function refresh() {
			batchSignatureForm.refresh();
			return false; // prevent postback
		}
	</script>

</asp:Content>