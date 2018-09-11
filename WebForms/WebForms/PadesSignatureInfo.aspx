<%@ Page Title="Pades Signature" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="PadesSignatureInfo.aspx.cs" Inherits="WebForms.PadesSignatureInfo" %>

<%@ PreviousPageType VirtualPath="~/PadesSignature.aspx" %>

<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">
	<h2>PAdES Signature</h2>

	<p>File signed successfully!</p>

	<p>User certificate information:</p>
	<ul>
		<li>Subject: <%= certificate.SubjectName.CommonName %></li>
		<li>Email: <%= certificate.EmailAddress %></li>
		<li>ICP-Brasil fields
			<ul>
				<li>Tipo de certificado: <%= certificate.PkiBrazil.CertificateType %></li>
				<li>CPF: <%= certificate.PkiBrazil.CPF %></li>
				<li>Responsavel: <%= certificate.PkiBrazil.Responsavel %></li>
				<li>Empresa: <%= certificate.PkiBrazil.CompanyName %></li>
				<li>CNPJ: <%= certificate.PkiBrazil.Cnpj %></li>
				<li>RG: <%= certificate.PkiBrazil.RGNumero %> <%= certificate.PkiBrazil.RGEmissor %> <%= certificate.PkiBrazil.RGEmissorUF %></li>
				<li>OAB: <%= certificate.PkiBrazil.OabNumero%> <%= certificate.PkiBrazil.OabUF %></li>
			</ul>
		</li>
	</ul>

	<h3>Actions:</h3>
	<ul>
		<li><a href="Download?file=<%= signatureFile.Replace(".", "_") %>">Download the signed file</a></li>
		<li><a href="PrinterFriendlyVersion?file=<%= signatureFile %>">Download a printer-friendly version of the signed file</a></li>
		<li><a href="OpenPadesSignature?userfile=<%= signatureFile.Replace(".", "_") %>">Open/validate the signed file</a></li>
	</ul>
</asp:Content>
