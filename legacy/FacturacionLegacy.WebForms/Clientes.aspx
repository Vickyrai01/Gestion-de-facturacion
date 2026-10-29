<%@ Page Title="Clientes" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Clientes.aspx.cs" Inherits="FacturacionLegacy.WebForms.Clientes" %>
<asp:Content ID="contenidoClientes" ContentPlaceHolderID="ContenidoPrincipal" runat="server">
    <h2>Gestión de clientes</h2>
    <p class="page-description">Registrá un cliente y consultá el listado de clientes registrados.</p>
    <section class="panel" aria-labelledby="tituloRegistro">
    <h3 id="tituloRegistro">Registrar cliente</h3>
    <div class="form-fields">
    <p class="field">
        <asp:Label ID="lblNombre" runat="server" AssociatedControlID="txtNombre" Text="Nombre" />
        <asp:TextBox ID="txtNombre" runat="server" MaxLength="100" />
        <asp:RequiredFieldValidator ID="valNombre" runat="server" ControlToValidate="txtNombre"
            ValidationGroup="AltaCliente" ErrorMessage="Ingresá el nombre." Display="Dynamic" />
    </p>
    <p class="field">
        <asp:Label ID="lblEmail" runat="server" AssociatedControlID="txtEmail" Text="Email" />
        <asp:TextBox ID="txtEmail" runat="server" MaxLength="254" />
        <asp:RequiredFieldValidator ID="valEmail" runat="server" ControlToValidate="txtEmail"
            ValidationGroup="AltaCliente" ErrorMessage="Ingresá el email." Display="Dynamic" />
    </p>
    </div>
    <asp:Button ID="btnGuardar" runat="server" CssClass="button-primary" Text="Guardar" ValidationGroup="AltaCliente" OnClick="btnGuardar_Click" />
    <p class="result" role="status"><asp:Label ID="lblResultado" runat="server" /></p>
    </section>
    <section class="panel" aria-labelledby="tituloListado">
    <h3 id="tituloListado">Clientes registrados</h3>
    <div class="table-container">
    <asp:GridView ID="grdClientes" runat="server" CssClass="clients-table" GridLines="None" CellSpacing="0" AutoGenerateColumns="false" EmptyDataText="Todavía no hay clientes." EnableViewState="true">
        <Columns>
            <asp:BoundField DataField="Id" HeaderText="Id" HtmlEncode="true" />
            <asp:BoundField DataField="Nombre" HeaderText="Nombre" HtmlEncode="true" />
            <asp:BoundField DataField="Email" HeaderText="Email" HtmlEncode="true" />
        </Columns>
    </asp:GridView>
    </div>
    </section>
</asp:Content>
