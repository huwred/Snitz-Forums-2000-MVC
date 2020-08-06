$(window).on("load", function(){
    // Insert Emoticon
    $("#editorDiv").on('click', '.emote', function (event) {

        var emotiText = $(event.target).attr("rel");
        //get the id of the textarea
        var parentDiv = $(this).closest("[id^=editorDiv]");
        var textId = parentDiv.find(".bbc-code-editor")[0].id;
        $("#" + textId).insertAtCaret(emotiText);
    });

    // Editor Buttons
    $('body').off('click').on('click','.btn-postform',function (event) {

        var val1 = $(this).attr('data-first');
        var val2 = $(this).attr('data-last');
        var label = $(this).attr('data-label');
        var parentDiv = $(this).closest("[id^=editorDiv]");
        //debugger;
        //get the id of the textarea
        var textId = parentDiv.find(".bbc-code-editor")[0].id;
        
        if ($('#FormatMode').length === 0) {
            //no FormatMode control so just insert tags
            if (label === "listitem" || label === "numbered" || label === "alpha" || label === "unordered") {
                var inputText1 = $("#" + textId).getSelection();
                var lines1 = inputText1.split(/\n/);
                var opttext1 = "";

                for (var ii in lines1) {
                    opttext1 += "[*]" + lines1[ii] + "[/*]";
                }
                $("#" + textId).replaceSelection(val1.replace("[*]", "") + opttext1 + val2.replace("[/*]", ""));

            } else {
                $("#" + textId).surroundSelection(val1, val2);

            }
            $("#" + textId).focus();
        } else if ($('#FormatMode').val() === 'Help') {
            //Display help for Tag
            var test = $(this).html();
            BootstrapDialog.show({
                title: window.Snitzres.helpEditor,
                message: $(this).next().next('.btn-help').html(),
                size: BootstrapDialog.SIZE_NORMAL
            });
        }else if ($('#FormatMode').val() === 'Basic' || label.length < 1) {
            if (label === "listitem" || label === "numbered" || label === "alpha" || label === "unordered") {
                var inputText = $("#" + textId).getSelection();
                var lines = inputText.split(/\n/);
                var opttext = "";
                
                for (var i in lines) {
                    opttext += "[*]" + lines[i] + "[/*]";
                }
                $("#" + textId).replaceSelection(val1.replace("[*]", "") + opttext + val2.replace("[/*]", ""));

            } else {
                $("#" + textId).surroundSelection(val1, val2);
                                
            }
            $("#" + textId).focus();
        }  else {
            //Display prompt input for tag
            event.preventDefault();
            //if we have selected anything, copy it to the input field
            var url = SnitzVars.baseUrl + "Topic/PromptDialog"; // the url to the controller
            if (label !== '') {
                $.get(url + '?data=' + label, function (data) {
                    $('#prompt-container').html(data);
                    $('#prompt-input').val($("#" + textId).getSelection());

                    //pass the tags to the dialog
                    $('#val1').val(val1);
                    $('#val2').val(val2);
                    $('#promptModal').modal().show();
                });                
            }

        }
        return false;
    });
    //font selectors
    $(".sel-postform").on('click', function (event) {
        var val1 = $(this).attr('data-first');
        var val2 = $(this).attr('data-last');
        var label = $(this).attr('data-label');
        var parentDiv = $(this).closest("[id^=editorDiv]");
        //get the id of the textarea
        var textId = parentDiv.find(".bbc-code-editor")[0].id;
        event.preventDefault();
        if ($('#FormatMode').length === 0) {
            //no FormatMode control so just insert tags
            $("#" + textId).surroundSelection(val1.replace("XXX", this.title), val2.replace("XXX", this.title));
            $("#" + textId).focus();

        } else if ($('#FormatMode').val() === 'Basic' || label.length < 1) {

            $("#" + textId).surroundSelection(val1.replace("XXX", this.title), val2.replace("XXX", this.title));
            $("#" + textId).focus();
        } else if ($('#FormatMode').val() === 'Help') {
            
            //Display help for Tag
            BootstrapDialog.show({
                title: window.Snitzres.helpEditor,
                message: $(this).parent().parent().next().html().replace(/XXX/g, this.title).replace(/inherit/g, this.title),
                size: BootstrapDialog.SIZE_NORMAL
            });
            $(this)[0].selectedIndex = 0;
        } else {
            //Display prompt input for tag
            var selectedVal = this.title;
            //if we have selected anything, copy it to the input field
            var url = SnitzVars.baseUrl + "Topic/PromptDialog"; // the url to the controller

            $.get(url + '?data=' + label + "&selected=" + selectedVal, function (data) {
                $('#prompt-container').html(data);
                $('#prompt-input').val($("#" + textId).getSelection());
                
                //pass the tags to the dialog
                $('#val1').val(val1.replace("XXX", selectedVal));
                $('#val2').val(val2.replace("XXX", selectedVal));
                $('#promptModal').modal().show();
            });
        }


        $("#" + textId).focus();

    });

    //bind the submit event for the tag prompt dialog
    $('body').on('click', '.prompt-sub-btn', function () {
        var parentDiv = $(this).closest("[id^=editorDiv]");
        var textId = parentDiv.find(".bbc-code-editor")[0].id;
        var val1 = $('#val1').val();
        var val2 = $('#val2').val();
        var process = $('#process').val();
        var inputText = $('#prompt-input').val();

        if (process === "link") {
            var url = $('#prompt-url').val();
            if (inputText.length > 0) {
                val1 = val1.replace(']', '="' + url + '"]');
                $("#" + textId).insertAtCaret(val1 + inputText + val2);
            } else {
                $("#" + textId).insertAtCaret(val1 + url + val2);
            }

        } else if (process === "email") {
            var url = $('#prompt-url').val();
            if (inputText.length > 0) {
                val1 = val1.replace(']', '="' + url + '"]');
                $("#" + textId).insertAtCaret(val1 + inputText + val2);
            } else {
                $("#" + textId).insertAtCaret(val1 + url + val2);
            }

        }else if (process == "list") {
            //split and wrap
            var lines = inputText.split(/\n/);
            var opttext = ""; for (var i in lines) {
                opttext += "[*]" + lines[i] + "[/*]";
            }
            $("#" + textId).insertAtCaret(val1.replace("[*]", "") + opttext + val2.replace("[/*]", ""));

        } else {
            //copy data to message and close the prompt dialog
            $("#" + textId).insertAtCaret(val1 + inputText + val2);
        }

        $('#promptModal').modal('hide');
        return false;
    });

    //bind the click event for preview dialog
    $('body').on('click', '.prev-link', function (e) {

        e.preventDefault();
        e.stopPropagation();
        e.stopImmediatePropagation();

        var sub = '';
        var msg = $('#Message').val().replace(/\\/g, '\\\\').replace(/"/g, '\\"');

        var sig = false;
        var id = $('#ReplyId').val();
        var tid = $('#TopicId').val();
        if ($('#Subject').length) {
             sub = $('#Subject').val();
        }

        if ($('#show-sig').is(':checked')) {
            sig = true;
        }
        
        $(this).tooltip('hide');
        
        $.ajax({
            type: "POST",
            url: window.SnitzVars.baseUrl + 'Forum/ProcessPreview',
            data: '{"Message":"' + msg + '","ShowSig":"' + sig + '","ReplyId":"' + id + '","TopicId":"' + tid + '","Subject":"' + sub + '"}',
            contentType: 'application/json; charset=utf-8',
            error: function (jqXHR, exception) {
                //
                BootstrapDialog.show({
                    title: 'Preview Error',
                    message: jqXHR.responseText,
                    size: BootstrapDialog.SIZE_WIDE
                });
            },
            success: function (data, textStatus, jqXHR) {
                var params = [
                    'height=' + screen.availHeight * 75 / 100,
                    'width=' + Math.min(screen.availWidth * 95 / 100, 800) ,
                    'scrollbars=1',
                    'resizable=1'
                ].join(',');
                var popup = window.open('', 'previewwindow', params);
                popup.moveTo(10, 10);
                popup.document.write(data);
                popup.document.close(jqXHR,status); // needed for chrome and safari
            }, complete: function (jqXHR, textStatus ){
                
            }
        });
        
    });

    // Attach listener to .modal-close-btn's so that when the button is pressed the modal dialog disappears
    $('body').on('click', '.modal-close-btn', function () {
        $('#modal-container').modal('hide');
        $('#promptModal').modal('hide');
        $("#Message").focus();
        
    });

    //clear modal cache, so that new content can be loaded
    $('#modal-container').on('hidden.bs.modal', function () {
        //
        $(this).removeData('bs.modal');

        $.get(window.SnitzVars.baseUrl + 'Home/RefreshToken', function (html) {
            var tokenValue = $('<div />').html(html).find('input[type="hidden"]').val();
            $('#afToken input[type="hidden"]').val(tokenValue);

        });
    });

    $('#promptModal').on('hidden.bs.modal', function () {
        $(this).removeData('bs.modal');
    });

});

AddAntiForgeryToken = function (data) {
    data.__RequestVerificationToken = $('#__AjaxAntiForgeryForm input[name=__RequestVerificationToken]').val();
    return data;
};
