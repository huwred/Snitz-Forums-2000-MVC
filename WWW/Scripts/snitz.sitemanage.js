$(document).ready(function () {

    // We can attach the `fileselect` event to all file inputs on the page
    $(document).on('change', ':file', function () {
        var input = $(this),
            numFiles = input.get(0).files ? input.get(0).files.length : 1,
            label = input.val().replace(/\\/g, '/').replace(/.*\//, '');
        input.trigger('fileselect', [numFiles, label]);
    });

    $(':file').on('fileselect', function (event, numFiles, label) {

        var input = $(this).parents('.input-group').find(':text'),
            log = numFiles > 1 ? numFiles + ' files selected' : label;

        if (input.length) {
            input.val(log);
        } else {
            if (log) alert(log);
        }

    });
    // check or uncheck all file checkboxes
    $('body').on('change', '#checkAll', function () {
        var checkboxes = $(this).closest('form').find(':checkbox');
        if ($(this).is(':checked')) {
            checkboxes.prop('checked', true);
        } else {
            checkboxes.prop('checked', false);
        }
    });
    //run query
    $('#runQuery').click(function (e) {
        e.preventDefault();
        $("#addRow").hide();
        var $form = $(this).parents('form');

        $.ajax({
            type: 'POST',
            url: $form.attr('action'),
            data: $form.serialize(),
            dataType: 'json',
            error: function (xhr, status, error) {
                $("#result").html('');
                $("#resultLabel").html(error + ": " + xhr.responseText);
                $("#resultLabel").addClass("danger");
            },
            success: function (response) {

                var jsonResultData =
                    ConvertJsonToTable(response,
                        null,
                        'table table-bordered table-striped table-condensed',
                        null);
                $("#result").html(jsonResultData);
                $("#resultLabel").html('Query Returned - ' + response.length + ' record(s)');
                $("#resultLabel").removeClass("danger");
                return false;
            }
        });
    });
    //delete handler
    $('body').on('click', 'a.del-col', function (e) {
        e.preventDefault();

        var $href = $(this).attr("href");

        $.get($href, function (data) {
            var resultData =
                ConvertJsonToTable($.parseJSON(data.Result),
                    null,
                    'table table-bordered table-striped table-condensed',
                    $href.replace(/delcolumn/gi, "EditTable").replace(/col=.*/gi, "col="));
            $("#result").html(resultData);
            $("#resultLabel").html(data.ID + ' Properties');
            $("#resultLabel").removeClass("danger");
        });

    });
    //row edit
    $('body').on('click', 'a.row-edit', function (e) {
        e.preventDefault();
        $.get($(this).attr("href"), function (data) {
            $('#myModalLabel').text('Edit Column');
            $('#editModal').modal('show');
            $('#editContainer').html(data);
            $.validator.unobtrusive.parse($("#editRowForm"));
        });
        return false;
    });
    //row add
    $('body').on('click', 'a.row-add', function (e) {
        e.preventDefault();
        $.get($(this).attr("href"), function (data) {
            $('#myModalLabel').text('Add Column');
            $('#editModal').modal('show');
            $('#editContainer').html(data);
            $.validator.unobtrusive.parse($("#editRowForm"));
        });
        return false;
    });
});
//File Manager functions
// create new folder
function newfolder() {

    if ($('#targetfolder').val() === '') {
        BootstrapDialog.alert({
            title: Snitzres.NoFolderErr,
            message: Snitzres.NoFolderMsg
        });

        $('#targetfolder').focus();
        return;
    }
    $('#filemanager-form').attr('action', SnitzVars.baseUrl + 'WebFileManager/newfolder');
    $('#filemanager-form').submit();
}
//upload files
function upload() {
    if ($('#fileupload').val() === '') {
        BootstrapDialog.alert({
            title: Snitzres.UploadErr,
            message: Snitzres.UploadNoFileMsg
        });
        $('#fileupload').focus();
        return;
    }

    $('#filemanager-form').attr('action', SnitzVars.baseUrl + 'WebFileManager/upload');
    $('#filemanager-form').attr('enctype', 'multipart/form-data');
    //enctype="multipart/form-data">
    $('#filemanager-form').submit();
}

// confirm file list and target folder
function confirmfiles(sAction) {
    var nMarked = 0;
    var sTemp = '';
    var checkboxes = $('#filemanager-form').find(':checkbox');
    checkboxes.each(function () {
        if (this.checked) {
            if (sAction === 'rename') {
                var sFilename = '';
                var sNewFilename = '';
                sFilename = this.name;
                sFilename = sFilename.replace('checked_', '');
                sNewFilename = prompt(window.Snitzres.NewFilename + sFilename, sFilename);

                if (sNewFilename != null) {
                    this.name = this.name + window.Snitzres.Rename + sNewFilename;
                    nMarked = nMarked + 1;
                } else {
                    $(this).prop("checked", false);
                }
            } else {
                nMarked = nMarked + 1;
            }

        }

    });

    if (nMarked === 0) {
        BootstrapDialog.newalert(window.Snitzres.fileSelectErr, window.Snitzres.fileSelectMsg);
        return;
    }
    sTemp = window.Snitzres.ConfirmMsg;
    var sTempTgt = window.Snitzres.ConfirmTarget;

    sTemp = sTemp.replace("[sAction]", sAction).replace("nMarked", nMarked);

    if (sAction === 'copy' || sAction === 'move') {
        sTempTgt = sTempTgt.replace("[folder]", $('#targetfolder').val());
        sTemp = sTemp + sTempTgt;
        if ($('#targetfolder').val() === '') {
            $('#targetfolder').focus();
            BootstrapDialog.alert({
                title: window.Snitzres.fileCopyErr, // 'Copy/Move Error',
                message: window.Snitzres.fileCopyMsg, //'No destination folder provided. Enter a folder name.'
            });
            return;
        }
    }
    sTemp = sTemp + "?";

    if (sAction === 'copy' || sAction === 'rename') {
        $('#filemanager-form').attr('action', window.SnitzVars.baseUrl + 'WebFileManager/' + sAction);
        $('#filemanager-form').submit();
    } else {
        //confirmed = confirm(sTemp);
        BootstrapDialog.confirm({
            title: window.Snitzres.Confirm, //'Confirmation Required',
            message: sTemp,
            callback: function (ok) {
                if (ok) {
                    $('#filemanager-form').attr('action', window.SnitzVars.baseUrl + 'WebFileManager/' + sAction);
                    $('#filemanager-form').submit();
                    return;
                }
            }
        });
    }

}

function linkSuccess(data) {

    var targetUrl = $(this)[0].href
        .replace('X-Requested-With=XMLHttpRequest', 'col=')
        .replace('/Table/', '/EditTable/');

    var resultData =
        ConvertJsonToTable($.parseJSON(data.Result),
            null,
            'table table-bordered table-striped table-condensed',
            targetUrl);
    $("#result").html(resultData);
    $("#resultLabel").html(data.ID + ' Properties');
    $("#resultLabel").removeClass("danger");
    $("#addNewRow").attr("href", targetUrl + "?col=NEW");
    $("#addRow").show();
    return false;
}

function linkFailure(data) {
    $("#result").html('');
    $("#resultLabel").html(data);
    $("#resultLabel").addClass("danger");
    $("#addRow").hide();
}