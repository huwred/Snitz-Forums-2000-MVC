
$(document).ready(function () {
    $(".numberinput").forceNumeric();
    $(".profile-text").on("click",
            function(e) {
                e.preventDefault();
                var profCtrl = "#input_" + $(this).data("control");
                saveProfile($(profCtrl).data("user"), $(profCtrl).attr("name"), $(profCtrl).val());
        });
    $(".profile-check").on("change",
        function () {
            if (this.checked) {
                saveProfile($(this).data("user"), $(this).attr("name"), 1);
            } else {
                saveProfile($(this).data("user"), $(this).attr("name"), 0);
            }
        });
    $(".profile-radio").on("change",
        function () {
            if (this.checked) {
                saveProfile($(this).data("user"), $(this).attr("name"), this.value);
            } else {
                saveProfile($(this).data("user"), $(this).attr("name"), this.value);
            }
        });
    $(".profile-tzselect").on("change",
        function () {
            //alert(this.value);
            saveProfile($(this).data("user"), $(this).attr("name"), this.value);
            //saveTimezone($(this).data("user"), this.value);
        });
    $(".profile-select").on("change",
        function () {
            alert($(this).attr("name"));
            saveProfile($(this).data("user"), $(this).attr("name") , this.value);
        });
     $("body").on("change","#FieldType",
            function() {
                if ($("#FieldType").val() === "O" || $("#FieldType").val() === "M" || $("#FieldType").val() === "H" || $("#FieldType").val() === "V") {
                    $("#options-create-box").show();
                } else {
                    $("#options-create-box").hide();
                }
            });
    $("body").on("keypress",".field-name",function(e) {
            var regex = new RegExp("^[a-zA-Z0-9]+$");
            var str = String.fromCharCode(!e.charCode ? e.which : e.charCode);
            if (regex.test(str)) {
                $(".field-name-err").hide();
                return true;
            }
            $(".field-name-err").show();
            e.preventDefault();
            return false;

        });

    $('.date').datetimepicker({
        dateFormat: 'dd/mm/yy',
        showTimepicker: false
    });
    $('.datetime').datetimepicker({
        dateFormat: 'dd/mm/yy',
        timeFormat: 'HH:mm'
    });
    $('.time').datetimepicker({
        dateFormat: 'dd/mm/yy',
        timeOnly: true
    });

    loadUserTimeZone();
    setInterval(function () {
        loadUserTimeZone(); // this will run after every minute
    }, 60000);
    setRequiredFields();
});
//function addOption() {
//    $("div.option-field:last").clone().insertAfter("div.option-field:last");

//}
function loadUserTimeZone() {
    if ($("#TimeZoneUpdate").length > 0) {
        $("#TimeZoneUpdate")
            .load(window.SnitzVars.baseUrl + "MemberFields/MemberField/" + window.SnitzVars.currentUserId + "?property=TimeZone");
    }
}

//function saveTimezone(user,timezone) {
//    var tz = $('#TimeZone');
//    $.ajax({
//        type: "POST",
//        url: window.SnitzVars.baseUrl + "MemberFields/SaveTimezone",
//        traditional: true,
//        data: {
//            id: user,
//            property: 'TimeZone',
//            propvalue: timezone
//        },
//        success: function (data) {
//            $("#TimeZoneDiv").html(data);
//        },
//        error: function () {
//            alert('error!');
//        }

//    });
//}

function saveProfile(user, field,value) {
    $.ajax({
        type: "POST",
        url: window.SnitzVars.baseUrl + "MemberFields/SaveProfile",
        traditional: true,
        data: {
            id: user,
            property: field,
            value: value
        },
        success: function (data) {
            $("#" + field).html(data);
        },
        error: function () {
            alert('error!');
        }

    });    
}

function saveProfileField() {
    $('#create-field-form').submit();
}
function fieldSuccess() {
    $("#modal-container").modal('hide');
    setTimeout(function () {
        window.location.reload();
    }, 500);
}

