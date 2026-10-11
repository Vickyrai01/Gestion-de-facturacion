(function ($) {
    function renumerar() {
        $('#detalles tbody tr').each(function (indice) {
            $(this).find('select').attr('name', 'Detalles[' + indice + '].ProductoId').attr('id', 'Detalles_' + indice + '__ProductoId');
            $(this).find('input').attr('name', 'Detalles[' + indice + '].Cantidad').attr('id', 'Detalles_' + indice + '__Cantidad');
        });
    }
    $('#agregar-detalle').on('click', function () {
        if ($('#detalles tbody tr').length >= 100) return;
        $('#detalles tbody').append($('#plantilla-detalle').html());
        renumerar();
    });
    $('#detalles').on('click', '.quitar-detalle', function () {
        $(this).closest('tr').remove();
        renumerar();
    });
    $('#form-factura').on('submit', renumerar);
})(jQuery);
