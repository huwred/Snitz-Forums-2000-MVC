$(document).ready(function () {
    $('body').on('click', '.modal-link', function (e) {
        e.preventDefault();
        $(this).tooltip('hide');

        $(this).attr('data-target', '#modal-container');
        $(this).attr('data-toggle', 'modal');
    });
    $(document).on('click', '#approve-btn', function () {
        $('#modal-container').modal('hide');
    });
    $('#checkAll').change(function () {
        var table = $(this).closest('table');
        $('td input:checkbox', table).prop('checked', this.checked);
    });
    $('.getModerators').on('change', function () {
        $('#forum-moderators').load(SnitzVars.baseUrl + 'Admin/GetForumModerators/' + $(this).val(), function () {
            $(".multiselect").twosidedmultiselect();
        });

    });
    $("#forum-moderators").on("click", "#select_all", function () {
        $('#ForumModerators option').prop('selected', this.checked);
    });
    $(".multiselect").twosidedmultiselect();
    $.fn.bootstrapSwitch.defaults.onColor = 'success';
    $.fn.bootstrapSwitch.defaults.offColor = 'warning';
    $("[name='yesno-checkbox']").bootstrapSwitch();
    $('.yesno-checkbox').bootstrapSwitch();

    $('*[data-autocomplete-url]').each(function () {
        $(this).autocomplete({
            source: $(this).data("autocomplete-url"),
            minLength: 3
        });
    });
    $('input[name="strallowuploads"]')
        .on('switchChange.bootstrapSwitch',
            function () {

                if ($(this).bootstrapSwitch('state')) {
                    $("#upload-options").fadeIn(1000);
                } else {
                    $("#upload-options").fadeOut('slow');
                }
        });

    $('input[name="strmanagefiles"]')
        .on('switchChange.bootstrapSwitch',
            function () {
                if ($(this).bootstrapSwitch('state')) {
                    $("#file-upload").fadeIn(1000);
                } else {
                    $("#file-upload").fadeOut('slow');
                }
        });

    $('.aspinEdit').spinedit();
    $('input[type=number]').numeric();
    $('.disabled').each(function () {
        $(this).prop("readonly", true);
    });
    $('body').on('click', '.save-lnk', function (event) {
        var newvalue = $(this).parent().parent().children('.col-xs-8').children()[0].value;

        var fakedUri = $(this).prop("href");
        var uri = fakedUri.replace("xxxxxx", newvalue);
        $(this).attr("href", uri);

    });
    $('body').on('click', '.add-lnk', function (event) {
        var newvalue = $(this).parent().parent().children('.col-xs-8').children()[0].value;
        var fakedUri = $(this).prop("href");
        var uri = fakedUri.replace("xxxxxx", newvalue);
        $(this).attr("href", uri);

    });
    $('#adminTab a').on('click', function (e) {
        $('#adminTab li.active').removeClass('active');
        $(this).parent('li').addClass('active');
        $.cookie('saved-tab', $(this).text());
    });

    BootstrapDialog.configDefaultOptions({ animate: false });

    if ($("#checkbox-auth").length > 0) {
        if ($("#checkbox-auth")[0].checked) {
            $("#smtp-auth").show();
        } else {
            $("#smtp-auth").hide();
        }
        $("#checkbox-auth").change(function() {
            if (this.checked) {
                $("#smtp-auth").show();
            } else {
                $("#smtp-auth").hide();
            }
        });
        if ($("#checkbox-cred")[0].checked) {
            $(".user-cred").hide();
        } else {
            $(".user-cred").show();
        }
        $("#checkbox-cred").change(function() {
            if (this.checked) {
                $(".user-cred").hide();
            } else {
                $(".user-cred").show();
            }
        });

        if ($("#delivery").val() == "Network") {
            $("#pickup-folder").hide();
        } else {
            $("#pickup-folder").show();
        }
        $("#delivery").change(function() {

            if ($(this).val() == "Network") {
                $("#pickup-folder").hide();
            } else {
                $("#pickup-folder").show();
            }
        });
    }
    //Ranking
    $("form").on("reset", function () {
        $("input[type='text'][id^='rankImage_'").each(function () {

            var id = $(this).attr('id');
            var lastChar = id.substr(id.length - 1);

            $("img[title='" + $(this).val() + "'][name='" + lastChar + "']").removeClass('selected');
            $("img[title='" + $(this)[0].defaultValue + "'][name='" + lastChar + "'").addClass('selected');
        });
    });

    $(".rank").on('click', function (e) {
        $("img[title='" + $("#rankImage_" + $(this).attr("name")).val() + "'][name='" + $(this).attr("name") + "']").removeClass('selected');
        $(this).removeClass('rank');
        $(this).addClass('rank selected');
        $("#rankImage_" + $(this).attr("name")).val($(this).attr("title"));
    });
    //Subs
    $(".remove-sub").on("click", function () {
        var data = {};

        data.id = $(this).attr("data-id");

        $.ajax({
            url: SnitzVars.baseUrl + 'Admin/RemoveSubscription',
            data: data,
            type: "POST",
            cache: false,
            success: function (response) {
 
                location.replace(response);
            },
            error: function (jqXHR, textStatus, errorThrown) {
                BootstrapDialog.show({
                    type: BootstrapDialog.TYPE_WARNING,
                    title: textStatus,
                    message: errorThrown
                });

            }
        });
    });
    $('body').on('click', '#remove-subscriptions', function (evt) {
        evt.preventDefault();
        BootstrapDialog.confirm({
            title: 'Delete Subscriptions',
            message: '<label>Are you sure you want to delete the subscriptions? </label><br/>', callback: function (ok) {
                if (ok) $('#del-subscription-form').submit();
            }
        });

    });
    //roles
    $(".roleLink").on("click", function () {
        $("#role-panel").html('<i class="fa fa-spinner></i>');
        var role = $(this).text();
        $.get(SnitzVars.baseUrl + 'Admin/GetRoleView/?rolename=' + role, function (data) {
            $(".validation-summary-errors").hide();
            $("#role-panel").html(data);
            $('#IsRolenameRequired').resetValidation();
            $.validator.unobtrusive.parse($("#role-panel"));
            $("#userNameLookup").autocomplete({ source: SnitzVars.baseUrl + 'Home/AutoCompleteUsername', minLength: 3 });
        });
    });

    if (activePage.length > 1) {
        var liL = $('#adminTab li');
        $('#adminTab li.active').removeClass('active');
        var hidnData = activePage;
        liL.each(function () {
            if ($(this).find('a').text() == hidnData) {

                $(this).addClass('active');
            }
        });
    }
    if (activeTab.length > 1) {
        $('[href="' + activeTab + '"]').tab('show');
    }
    if (errMsg.length > 1) {
        BootstrapDialog.show({
            type: BootstrapDialog.TYPE_WARNING,
            title: errTitle,
            message: errMsg
        });
    }
    if (succMsg.length > 1) {
        BootstrapDialog.show({
            type: BootstrapDialog.TYPE_SUCCESS,
            title: succTitle,
            message: succMsg
        });
    }
    $('body').on('click', '#submitBanner', function (evt) {
        evt.preventDefault();
        var formdata = new FormData($(this).closest('form').get(0)); //FormData object
        var fileInput = document.getElementById('fileInput');
        //Iterating through each files selected in fileInput
        //There is currently only one
        for (var i = 0; i < fileInput.files.length; i++) {
            //Append each file to FormData object
            formdata.append(fileInput.files[i].name, fileInput.files[i]);
            if (fileInput.files[i].size > (SnitzVars.MaxFileSize * 1024 * 1024)) {
                BootstrapDialog.alert(
                {
                    title: "Error ",
                    message: 'File is too large.'
                });
                return false;
            }
        }

        //Create an XMLHttpRequest and post it
        var xhr = new XMLHttpRequest();
        xhr.open('POST', SnitzVars.baseUrl + 'Ad/AddEdit');
        xhr.send(formdata);
        xhr.onerror = function (ev) {
            BootstrapDialog.alert(
            {
                title: "Error",
                message: ev.error
            });
        }
        xhr.onreadystatechange = function () {
            if (xhr.readyState === 4 && xhr.status === 200) {
                var arr = xhr.responseText.split('|');
                if (arr[0] === "error") {
                    BootstrapDialog.alert(
                    {
                        title: "Error ",
                        message: 'Error uploading'
                    });
                } else {

                    $('#modal-container').modal('hide');
                    window.location.href = xhr.responseText.replace(/['"]+/g, '');
                    return false;
                }

            }
        }
        return false;
    });

});

function OnSuccess(response) {
    //
    $(".yesno-checkbox").bootstrapSwitch();
    $("input[type=submit]").removeAttr('disabled');

}

function OnFailure(ajaxContext) {
    var response = ajaxContext.responseText;

    BootstrapDialog.show({
        type: BootstrapDialog.TYPE_WARNING,
        title: 'Error',
        message: response
    });
}