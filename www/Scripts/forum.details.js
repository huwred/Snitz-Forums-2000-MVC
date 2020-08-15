$(window).on('load', function () {
    $('*[data-autocomplete-url]').each(function () {
        $(this).autocomplete({
            source: $(this).data("autocomplete-url"),
            minLength: 3
        });
    });
    $("#btn-submit").on('click', function (e) {
        $('#AllowedMembers option').prop('selected', true);
    });
    $('.aspinEdit').spinedit();
    
    setConditionalValidators();

    if ($("#Type").val() == 'WebLink') {

        $("#forumLink").show();
        $("#default-days").hide();
        $("#increase-posts").hide();
        $("#forum-props").hide();
        $("#cal-forumAuth").parent().hide();

    } else {
        $("#forumLink").hide();
        $("#forumSubject").show();
        $("#default-days").show();
        $("#increase-posts").show();
        $("#forum-props").show();
    }
    $("#forumModeration").on("change", function () {
        var notModerated = $(this).val() === "UnModerated";
        if (notModerated) {
            $(".tsmsoptions .RemoveAll").click();
            $("#forum-moderators").hide();
        } else {

            $("#forum-moderators").show();
        }
    });

    $("#private-forums").on("change", function () {
        var isAllowed = /^Allowed.*$/.test($(this).val());
        var usePassword = /.*Password.*$/.test($(this).val());

        if (isAllowed)
        { $("#allowed-members").show(); } else {
            $('#AllowedMembers').empty();
            $("#allowed-members").hide();
        }
        if (usePassword) {
            $("#forum-password").show();
        } else {
            $("#forum-password").empty();
            $("#forum-password").hide();
        }
    });

    $("#Type").on("change", function () {
        var selected = $(this).val();
        if (selected == 'WebLink') {
            //$("#forumSubject").hide();
            $("#forumLink").show();
            $("#default-days").hide();
            $("#increase-posts").hide();
            $("#forum-props").hide();
        } else {
            $("#forumLink").hide();
            $("#forumSubject").show();
            $("#default-days").show();
            $("#increase-posts").show();
            $("#forum-props").show();
        }
    });

    $("#chkShowPassword").on('click', function () {
        var txtPassword = $("#PasswordNew");
        if ($(this).is(":checked")) {
            txtPassword.after('<input onchange = "PasswordChanged(this);" id = "txt_' + txtPassword.attr("id") + '" type = "text" value = "' + txtPassword.val() + '" class="form-control" />');
            txtPassword.hide();
        } else {
            txtPassword.val(txtPassword.next().val());
            txtPassword.next().remove();
            txtPassword.show();
        }
    });
    $("#add-allowed").on('click', function (e) {
        e.preventDefault();
        $('#AllowedMembers').append($('<option/>', {
            value: $("#new-allowed").val(),
            text: $("#new-allowed").val()
        }));
        $("#new-allowed").val('');
    });
    $("#rem-allowed").on('click', function (e) {
        e.preventDefault();
        $('#AllowedMembers').find('option:selected').remove();

    });
});

function PasswordChanged(txt) {
    $(txt).prev().val($(txt).val());
}
