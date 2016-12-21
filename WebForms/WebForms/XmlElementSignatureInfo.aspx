<%@ Page Title="Xml Element Signature" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="XmlElementSignatureInfo.aspx.cs" Inherits="WebForms.XmlElementSignatureInfo" %>
<%@ PreviousPageType VirtualPath="~/XmlElementSignature.aspx" %>

<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">
	<h2>Xml Element Signature</h2>

	<p>File signed successfully! <a href="Download?file=<%= signatureFile.Replace(".", "_") %>">Click here to download the signed file</a></p>

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
</asp:Content>