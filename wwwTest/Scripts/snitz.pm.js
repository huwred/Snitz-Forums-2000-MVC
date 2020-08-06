function updateCount() {
    $.ajax({
        url: window.SnitzVars.baseUrl + 'PrivateMessage/MailboxLimitString',
        error: function (data) {
            alert('Update count error: ' + data.responseText);
        },
        success: function (result) {
            //alert(result);
            $('#mailbox-size').html(result);
        }

    });    
}
var PmSentOK = function (data) {
    updateCount();   
    //debugger;
    var dlg = window.BootstrapDialog.show({
        type: window.BootstrapDialog.TYPE_PRIMARY,
        title: 'Private Messaging',
        message: data.param2
    });
    setTimeout(function () {
        dlg.close();
        window.location.replace(data.param3);

    }, 1500);


}
var PmSendFail = function (parameters) {
    debugger;
    $('.pm-send').show();
    $('body').one('click', '.pm-send', bindpmsend);
    $('#message-box').html(parameters.responseText);
}
var PmDeleteOK = function () {
    updateCount();
    var dlg = window.BootstrapDialog.show({
        type: window.BootstrapDialog.TYPE_PRIMARY,
        title: 'Private Messaging',
        message: 'Message deleted'
    });
    setTimeout(function () {
        dlg.close();
        
    }, 1500);

}
bindpmsend = function (evt) {
    evt.preventDefault();
    window.showDelete = true;
    if ($('#message-form').valid() && !($(this).attr('disabled'))) {
        $(this).attr('disabled', 'disabled');
        $('.pm-send').hide();
        if (window.console) {
            console.log('post-PM');
        }
        $('#message-form').submit();
    } else {
        $(this).removeAttr('disabled');
        $('body').unbind('click', '.pm-send', bindpmsend);
        $('body').one('click', '.pm-send', bindpmsend);
    }
} 
bindpmdraft = function (evt) {
    evt.preventDefault();
    window.showDelete = true;

    if ($('#message-form').valid() && !($(this).attr('disabled'))) {
        $(this).attr('disabled', 'disabled');
        $('.pm-draft').hide();
        $('#message-form').submit();
    } else {
        $(this).removeAttr('disabled');
        $('body').unbind('click', '.pm-send', bindpmsend);
        $('body').one('click', '.pm-send', bindpmsend);
    }
} 
$(window).on("load", function(){
            

    $('body').on('change', '#checkAll', function() {
        var checkboxes = $(this).closest('form').find(':checkbox');
        if ($(this).is(':checked')) {
            checkboxes.prop('checked', true);
        } else {
            checkboxes.prop('checked', false);
        }
    });
    $('#message-panel').hide();
    var $loading = $('#msgloading').hide();
    $(document)
        .ajaxStart(function () {
            if (!window.loading) {
                $loading.show();
            }
            
            window.loading = true;
        })
        .ajaxStop(function () {
            $loading.hide();
            window.loading = false;
        });

    $('#message-box').on('click', '.getMessage', function (e) {
        window.loading = true;
        var clickedcell = $(e.target).closest('td');
        
        if ($(clickedcell).text() === '') {
            return true;
        }
        
        $(".getMessage").removeClass('selected');
        $(this).addClass('selected');
        $('#message-panel').load(window.SnitzVars.baseUrl + 'PrivateMessage/GetMessage/' + $(this).attr("data-val") + '?inbox=' + $(this).attr("data-inbox"));
        $('#message-panel').show();
        $('body').unbind('click', '.pm-send', bindpmsend);

        if ($(this).attr("data-read") === "-1") {
            $('.pm-send').show();
            $('body').one('click', '.pm-send', bindpmsend);
            $('.pm-attach').show();
            window.showDelete = false;            
        }
        if ($(this).attr("data-read") === "0") {
            $('.pm-send').show();
            $('body').one('click', '.pm-send', bindpmsend);
            $('.pm-attach').show();
            window.showDelete = false;
        }
        try {
            $('html, body').animate({
                scrollTop: ($('#page-top').offset().top) + 'px' // - 150
            }, 500, 'swing');
        } catch (e) {
                //trap any errors

        }

    });

    $('body').on('mouseup', '.msg-selectall', function () {
        var checkboxes = $(this).closest('form').find(':checkbox');
        if ($(this).is(':checked')) {
            checkboxes.prop('checked', true);
        } else {
            checkboxes.prop('checked', false);
        }
    });

    $('body').on('change', '.msg-draft', function () {
        
        //var icon = $(".pm-send").find('i');
        //$(".pm-send").attr("title", "Save as draft");

        if (this.checked) {
            $('.pm-send').hide();
            $('.pm-draft').show();
            $('body').one('click', '.pm-draft', bindpmdraft);

            //icon.removeClass("fa-send-o");
            //icon.addClass("fa-save");
        } else {
            //icon.removeClass("fa-save");
            //icon.addClass("fa-send-o");
            $('.pm-send').show();
            $('.pm-draft').hide();
            $('body').one('click', '.pm-send', bindpmsend);
        }
    });

           

$('body').on('click', '.pm-delete', function (evt) {
    evt.preventDefault();
    if ($("#del-message-form input:checkbox:checked").length === 0) {
        window.BootstrapDialog.alert(
        {
            title: window.Snitzres.Warning,
            message: window.Snitzres.PMNoSelection
        });
        return false;
    }

    window.BootstrapDialog.confirm({
        title: window.Snitzres.DeletePM,
        message: window.Snitzres.PMDelConfirm, callback: function (ok) {
            if (ok) $('#del-message-form').submit();
        }
    });
    
});

$('body').on('click', '.pm-button', function(event) {
    event.preventDefault();
    $(this).tooltip('hide');
    $('#message-panel').empty();
    $('#message-panel').hide();
    //debugger;

    $('body').unbind('click', '.pm-send', bindpmsend);
    if ($(this).attr("data-target") === "NewMessage") {

        $('.pm-send').show();
        $('body').unbind('click', '.pm-send', bindpmsend);
        $('body').one('click', '.pm-send', bindpmsend);
        $('.pm-attach').show();
        window.showDelete = false;
    } else if ($(this).attr("data-target") === "ReplyMessage") {
        $('.pm-send').show();
        $('body').unbind('click', '.pm-send', bindpmsend);
        $('body').one('click', '.pm-send', bindpmsend);
        $('.pm-attach').show();
        window.showDelete = false;
    } else if ($(this).attr("data-target") === "ForwardMessage") {
        $('.pm-send').show();
        $('body').unbind('click', '.pm-send', bindpmsend);
        $('body').one('click', '.pm-send', bindpmsend);
        $('.pm-attach').show();
        window.showDelete = false;
    } else if ($(this).attr("data-target") === "Settings") {
        $('.pm-send').hide();
        $('body').unbind('click', '.pm-send', bindpmsend);
        //$('body').off('click', '.pm-send', bindpmsend);
        $('.pm-attach').hide();
        window.showDelete = false;
        $('#message-panel').load(window.SnitzVars.baseUrl + 'PrivateMessage/Blocklist/' + $(this).attr("data-val"));
        $('#message-panel').show();

    } else if ($(this).attr("data-target") === "GetFolder") {
        window.showDelete = true;
        $('.pm-send').hide();
        $('body').unbind('click', '.pm-send', bindpmsend);
        //$('body').off('click', '.pm-send', bindpmsend);
        $('.pm-attach').hide();
    } else if ($(this).attr("data-target") === "SearchMessages") {
        $('.pm-send').hide();
        $('body').unbind('click', '.pm-send', bindpmsend);
        //$('body').off('click', '.pm-send', bindpmsend);
        $('.pm-attach').hide();
        window.showDelete = false;
        $('#message-panel').html('');
        $('#message-panel').show();
    } else {
        $('.pm-send').hide();
        $('body').unbind('click', '.pm-send', bindpmsend);
        //$('body').off('click', '.pm-send', bindpmsend);
        $('.pm-attach').hide();
    }


    $('#message-box').load(window.SnitzVars.baseUrl + 'PrivateMessage/' + $(this).attr("data-target") + '/' + $(this).attr("data-val"));


});

$(".yesno-checkbox").bootstrapSwitch();
if (showDelete) {
    $('.pm-delete').show();
} else {
    $('.checkbox').hide();
    $('.pm-delete').hide();
}
});