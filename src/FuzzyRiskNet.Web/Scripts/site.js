// Write your Javascript code.

$(document).ready(function () {
    $('.mvcformshelperdialog').each(function () {
        var $link = $(this);
        var item = $('<div></div>');
        //$('#maincontent').append(item);

        var $dialog = item
            .dialog({
                autoOpen: false, modal: true,
                title: $link.attr('title'),
                width: 770,
                height: 'auto', position: ['center', 50]
            });

        var $onreturn = function () {
            if ($dialog.find("#returnresult").length > 0 && $link.data('putreturnresult').length > 0) {
                $(document.getElementById($link.data('putreturnresult'))).val($dialog.find("#returnresult").html());
                $dialog.dialog('close');
            }
        };

        $link.click(function () {
            $dialog.load($link.attr('href') + " #maincontent").dialog('open');

            return false;
        });

        $dialog.delegate('a', 'click', function (e) { $dialog.load($(this).attr('href') + " #maincontent", $onreturn); e.preventDefault(); });
        $dialog.delegate('form', 'submit', function (e) { e.preventDefault(); $dialog.load($(this).attr('action') + " #maincontent", $(this).serializeArray(), $onreturn); });
    });
});
