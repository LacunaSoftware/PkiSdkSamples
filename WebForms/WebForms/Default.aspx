<%@ Page Title="Home" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="WebForms._Default" %>

<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">

	<h2>Lacuna PKI SDK samples in ASP.NET Web Forms</h2>

	Choose one of the following samples:
	<ul>
		<li><a runat="server" href="~/Authentication">Authentication with digital certificate</a></li>
		<li>PAdES signature
		<ul>
			<li><a href="/PadesSignature">Create a signature with a file already on server</a></li>
			<%--<li><a href="/Upload?rc=PadesSignature">Create a signature with a file uploaded by user</a></li>
			<li><a href="/Upload?rc=OpenPadesSignature">Open/validate an existing signature</a></li>--%>
		</ul>
		</li>
		<li>CAdES signature
		<ul>
			<li><a runat="server" href="~/CadesSignature">Create a signature with a file already on server</a></li>
			<%--<li><a href="/Upload?rc=CadesSignature">Create a signature with a file uploaded by user</a></li>
			<li><a href="/Upload?rc=OpenCadesSignature">Open/validate an existing CAdES signature</a></li>--%>
			<li><a runat="server" href="~/PdfMarksFromCades">Create a PDF with marks from its CAdES signature</a></li>
		</ul>
		</li>
		<li>XML signature
		<ul>
			<%--<li><a href="/XmlFullSignature">Create a full XML signature (enveloped signature)</a></li>--%>
			<li><a runat="server" href="~/XmlElementSignature">Create a XML element signature</a></li>
		</ul>
		</li>
		<li>
		Sign a batch of files
		<ul>
			<li><a href="/BatchPadesSignature">Simple batch of PAdES signatures</a></li>
            <li><a href="/BatchCadesSignature">Simple batch of CAdES signatures</a></li>
			<%--<li><a href="/BatchSignatureOptimized">Optimized batch signature</a> (better performance but more complex Javascript)</li>--%>
		</ul>
	</li>
	</ul>


</asp:Content>
