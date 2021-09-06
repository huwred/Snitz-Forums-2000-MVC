$.extend({
    getUrlParams: function () {
        var vars = [], hash;
        var hashes = window.location.href.slice(window.location.href.indexOf('?') + 1).split('&');
        for (var i = 0; i < hashes.length; i++) {
            hash = hashes[i].split('=');
            vars.push(hash[0]);
            vars[hash[0]] = hash[1];
        }
        return vars;
    },
    getUrlParam: function (name) {
        return $.getUrlParams()[name];
    }
});
$.fn.extend({
    // form validation {
    isValid: function () {
        var self = $(this);
        $.validator.unobtrusive.parse(self);
        return self.data('unobtrusiveValidation').validate();
    }, // }

    //re-set all client validation given a jQuery selected form or child
    resetValidation: function () {

        var $form = this.closest('form');

        //reset jQuery Validate's internals
        $form.validate().resetForm();

        //reset unobtrusive validation summary, if it exists
        $form.find("[data-valmsg-summary=true]")
            .removeClass("validation-summary-errors")
            .addClass("validation-summary-valid")
            .find("ul").empty();

        //reset unobtrusive field level, if it exists
        $form.find("[data-valmsg-replace]")
            .removeClass("field-validation-error")
            .addClass("field-validation-valid")
            .empty();

        return $form;
    }
});
// functions to insert/select text in textarea
$.fn.extend({
    insertAtCaret: function (myValue) {
        var textComponent = $(this)[0];
        if (document.selection !== undefined) {
            console.log('insert 1');
            this.focus();
            sel = document.selection.createRange();
            console.log(sel.text);
            sel.text = myValue;
            console.log('set focus');
            this.focus();
        } else if (textComponent.selectionStart != undefined) {
            console.log('insert 2');
            var startPos = textComponent.selectionStart;
            var endPos = textComponent.selectionEnd;
            var scrollTop = textComponent.scrollIntoView.scrollTop;
            textComponent.value = textComponent.value.substring(0, startPos) + myValue + textComponent.value.substring(endPos, textComponent.value.length);

            textComponent.selectionStart = startPos + myValue.length;
            textComponent.selectionEnd = startPos + myValue.length;
            textComponent.blur();
            textComponent.focus();


        } else {
            console.log('insert 3');
            this.val(this.val() + myValue);
            this.focus();
        }
    },
    surroundSelection: function (val1, val2) {
        var selectedText = '';
        var textComponent = $(this)[0]; 
        if (document.selection != undefined) {
            
            this.focus();
            var sel = document.selection.createRange();
            sel.text = val1 + sel.text + val2;
            this.focus();
        }
            // Mozilla version
        else if (textComponent.selectionStart != undefined) {
            //textComponent.focus();
            var startPos = textComponent.selectionStart;
            var endPos = textComponent.selectionEnd;
            var scrollTop = textComponent.scrollTop;
            var myValue = val1 + val2;

            if (startPos !== endPos) {
                selectedText = textComponent.value.substring(startPos, endPos);
            
                textComponent.value = textComponent.value.substring(0, startPos) + val1 + selectedText + val2 + textComponent.value.substring(endPos, textComponent.value.length);

            } else {
                textComponent.value = textComponent.value.substring(0, startPos) + myValue + textComponent.value.substring(endPos, textComponent.value.length);
                //textComponent.value += val1 + val2;
            }
            textComponent.selectionStart = startPos + myValue.length;
            textComponent.selectionEnd = startPos + myValue.length;
            textComponent.blur();
            textComponent.focus();
        }
        

    },
    replaceSelection: function (val) {
        var selectedText = '';
        var textComponent = $(this)[0];
        //
        if (document.selection != undefined) {
            this.focus();
            var sel = document.selection.createRange();
            sel.text = val;
            this.focus();
        }
            // Mozilla version
        else if (textComponent.selectionStart != undefined) {
            textComponent.focus();
            var startPos = textComponent.selectionStart;
            var endPos = textComponent.selectionEnd;
            var scrollTop = textComponent.scrollTop;
            var myValue = val;

            if (startPos != endPos) {
                selectedText = textComponent.value.substring(startPos, endPos);

                textComponent.value = textComponent.value.substring(0, startPos) + val + textComponent.value.substring(endPos, textComponent.value.length);

            } else {
                textComponent.value = textComponent.value.substring(0, startPos) + myValue + textComponent.value.substring(endPos, textComponent.value.length);
                //textComponent.value += val1 + val2;
            }
            textComponent.selectionStart = startPos + myValue.length;
            textComponent.selectionEnd = startPos + myValue.length;
            textComponent.blur();
            textComponent.focus();

        }


    },
    getSelection: function () {
        var selectedText = '';
        var textComponent = $(this)[0];

        if (document.selection != undefined) {
            this.focus();
            var sel = document.selection.createRange();
            selectedText = sel.text;
            sel.text = "";
            this.focus();
        }
            // Mozilla version
        else if (textComponent.selectionStart != undefined) {
            textComponent.focus();
            var startPos = textComponent.selectionStart;
            var endPos = textComponent.selectionEnd;
            var scrollTop = textComponent.scrollTop;
            var myValue = "";

            if (startPos != endPos) {
                selectedText = textComponent.value.substring(startPos, endPos);

                textComponent.value = textComponent.value.substring(0, startPos) + textComponent.value.substring(endPos, textComponent.value.length);

            } else {
                textComponent.value = textComponent.value.substring(0, startPos) + myValue + textComponent.value.substring(endPos, textComponent.value.length);
                //textComponent.value += val1 + val2;
            }
            textComponent.selectionStart = startPos + myValue.length;
            textComponent.selectionEnd = startPos + myValue.length;
            textComponent.blur();
            textComponent.focus();

        }
        return selectedText;

    },
    selectRange:function (start, end) {
    if (!end) end = start;
    return this.each(function () {
        if (this.setSelectionRange) {
            this.focus();
            this.setSelectionRange(start, end);
        } else if (this.createTextRange) {
            var range = this.createTextRange();
            range.collapse(true);
            range.moveEnd('character', end);
            range.moveStart('character', start);
            range.select();
        }
    });
}
});

$.fn.popover.Constructor.prototype.reposition = function() {
    var $tip = this.tip();

    var placement = typeof this.options.placement === 'function'
        ? this.options.placement.call(this, $tip[0], this.$element[0])
        : this.options.placement;

    var autoToken = /\s?auto?\s?/i;
    var autoPlace = autoToken.test(placement);
    if (autoPlace) placement = placement.replace(autoToken, '') || 'top';

    var pos = this.getPosition();
    var actualWidth = $tip[0].offsetWidth;
    var actualHeight = $tip[0].offsetHeight;

    if (autoPlace) {
        var orgPlacement = placement;
        var viewportDim = this.getPosition(this.$viewport);

        placement = placement === 'bottom' && pos.bottom + actualHeight > viewportDim.bottom
            ? 'top'
            : placement === 'top' && pos.top - actualHeight < viewportDim.top
            ? 'bottom'
            : placement === 'right' && pos.right + actualWidth > viewportDim.width
            ? 'left'
            : placement === 'left' && pos.left - actualWidth < viewportDim.left
            ? 'right'
            : placement;

        $tip
            .removeClass(orgPlacement)
            .addClass(placement);
    }

    var calculatedOffset = this.getCalculatedOffset(placement, pos, actualWidth, actualHeight);

    this.applyPlacement(calculatedOffset, placement);
}

$.fn.forceNumeric = function () {
    return this.each(function () {
        $(this)
            .keydown(function (e) {
                var key = e.which || e.keyCode;

                if (!e.shiftKey &&
                    !e.altKey &&
                    !e.ctrlKey &&
                    // numbers   
                    key >= 48 &&
                    key <= 57 ||
                    // Numeric keypad
                    key >= 96 && key <= 105 ||
                    // comma, period and minus, . on keypad
                    key === 190 ||
                    key === 188 ||
                    key === 109 ||
                    key === 110 ||
                    // Backspace and Tab and Enter
                    key === 8 ||
                    key === 9 ||
                    key === 13 ||
                    // Home and End
                    key === 35 ||
                    key === 36 ||
                    // left and right arrows
                    key === 37 ||
                    key === 39 ||
                    // Del and Ins
                    key === 46 ||
                    key === 45)
                    return true;

                return false;
            });
    });
}

var styleCookieName = "snitztheme";
var styleCookieDuration = 60;
var styleDomain = "";
var timezoneCookie = "timezoneoffset";

function getLastPost(id, div_id){
    var cssdir = "text-left";
    var dir = "ltr";
    $.ajax({
        url: window.SnitzVars.baseUrl + "Reply/LastPost/" + id,
        success: function(response) {
            $('#' + div_id).html(response.Message);
            if (window.SnitzVars.forumlang === "fa" && window.persianRex.text.test(response.Message)) {
                cssdir = "text-right";
                dir = "rtl";
            }
            $('#' + div_id).addClass(cssdir);
        }
    });
    return '<div id="'+ div_id +'" class="lastpost-popover content message" dir="' + dir + '">Loading...' + id + '</div>';
}

function getPostMessage(id, div_id) {
    var cssdir = "text-left";
    var dir = "ltr";
    $.ajax({
        url: window.SnitzVars.baseUrl + "Topic/Subject/" + id,
        success: function(response) {
            $('#' + div_id).html(response.Message);
            if (window.SnitzVars.forumlang === "fa" && window.persianRex.text.test(response.Message)) {
                cssdir = "text-right";
                dir = "rtl";
            }
            $('#' + div_id).addClass(cssdir);
        }
    });
    //text-left

    return '<div id="'+ div_id +'" class="lastpost-popover content message" dir="' + dir + '">Loading...' + id + '</div>';
}

function pendingMemberShow(label) {
    var url = window.SnitzVars.baseUrl + 'Admin/PendingMembers';
    var dlg = BootstrapDialog.show({
        type: BootstrapDialog.TYPE_PRIMARY,
        title: 'Pending Members',
        message: '<a href="' + url + '"><i class="fa fa-users fa-1_5x animated infinite pulse"></i><span class="">' + label + '</span></a>'
    });
    setTimeout(function () {
        dlg.close();
    }, 5000);    
}

function errorDlg(title,error) {
    BootstrapDialog.show({
        type: BootstrapDialog.TYPE_WARNING,
        title: title,
        message: error
    });
}

function successDlg(title) {
    //var dlg = BootstrapDialog.successShow(title);
    var dialog = new BootstrapDialog({
        type: BootstrapDialog.TYPE_SUCCESS,
        title: title,
        message: function(dialogRef){
            var $message = $('<div>OK, this dialog has no header and footer, but you can close the dialog using this button: </div>');
            var $button = $('<button class="btn btn-primary btn-lg btn-block">Close the dialog</button>');
            $button.on('click', {dialogRef: dialogRef}, function(event){
                event.data.dialogRef.close();
            });
            $message.append($button);
        
            return $message;
        },
        closable: false
    });
    dialog.realize();
    dialog.getModalFooter().hide();
    dialog.getModalContent().css('min-height', '10px !important');
    dialog.getModalContent().css('height', '40px !important');
    dialog.getModalBody().hide();
    dialog.open();

}

function switchStyle(cssTitle) {
    var i, linkTag;
    for (i = 0, linkTag = document.getElementsByTagName("link"); i < linkTag.length; i++) {
        if ((linkTag[i].rel.indexOf("stylesheet") !== -1) &&
            linkTag[i].title) {
            linkTag[i].disabled = true;
            if (linkTag[i].title.toLowerCase() === cssTitle.toLowerCase()) {
                linkTag[i].disabled = false;
            }
        }
    }
}

var getBrowser = function() {
    var ua = navigator.userAgent, tem, M = ua.match(/(opera|chrome|safari|firefox|msie|trident(?=\/))\/?\s*(\d+)/i) || [];
    if (/trident/i.test(M[1])) {
        tem = /\brv[ :]+(\d+)/g.exec(ua) || [];
        return { name: 'IE', version: (tem[1] || '') };
    }
    if (M[1] === 'Chrome') {
        tem = ua.match(/\bOPR|Edge\/(\d+)/);
        if (tem != null) { return { name: 'Opera', version: tem[1] }; }
    }
    M = M[2] ? [M[1], M[2]] : [navigator.appName, navigator.appVersion, '-?'];
    if ((tem = ua.match(/version\/(\d+)/i)) != null) { M.splice(1, 1, tem[1]); }
    return {
        name: M[0],
        version: M[1]
    };
}

var executeCallback = function (tag) { //From ActionLinkConfirm
    $.ajax({
        type: 'GET',
        url: $(tag).attr("href"),
        success: function (response) {
            sessionStorage.setItem('scrollTop', $(window).scrollTop());
            successDlg(response.successMsg);
            window.location.replace(response.redirectUrl);
        },
        error: function (response) {
            window.errMsg = response;
        }
    });
}

var OnSuccess = function (response) {
    $("time.timeago").timeago();
}

var OnFailure = function (ajaxContext) {
    var response = ajaxContext.get_response();
    var statusCode = response.get_statusCode();
    errorDlg('Error: ' + statusCode, response);
}

var setConditionalValidators = function () {

    $.validator.unobtrusive.adapters.add(
            'conditional',
            ['dependentproperty', 'targetvalue'],
            function (options) {
                options.rules['conditional'] = {
                    conditional: options.params['dependentproperty'],
                    targetvalue: options.params['targetvalue']
                };
                options.messages['conditional'] = options.message;
            });

    $.validator.addMethod("conditional", function (value, element, parameters) {
        if (parameters['conditional'].match("^STRREQ")) {
            if (parameters['targetvalue'] === 'False') {
                return true;
            }
            /*we are required so check value*/
            if (element.value.length > 0) {
                return true;
            } else {
                return false;
            }

        }
        var ctl = $("[id$='" + parameters['conditional'] + "']");

        if (ctl.length > 0) {
            if (element.value.length > 0) {
                return true;
            }
            if (ctl.val() !== parameters['targetvalue']) {
                return true;
            }

        } else {
            return false;
        }
        return false;
    });
}

var tooltipPos = function (tip, element) { //$this is implicit
    var position = $(element).offset();
    var sWidth = $(window).width();
    var sHeight = $(window).height();

    if (position.left > sWidth * 0.75) {
        return "left";
    }
    if (position.left < sWidth * 0.75) {
        return "right";
    }
    if (position.top < 110) {
        return "bottom";
    }
    return "top";
}

var setRequiredFields = function() {
    $('form').find(':input').each(function () {
        var cond = $(this).attr('data-val-conditional-dependentproperty');
        var req = $(this).attr('data-val-required');
        var condreq = $(this).attr('data-val-conditional-targetvalue');
        if (undefined != req) {
            if ($(this).val() === '') {
                $(this).addClass('required-val'); //.css('background-color', '#F3D1DC');
            }
        }
        //
        var check = $(this);
        if (undefined != cond) {
            if ($(this).val() === '') {
                
                if (undefined != condreq && condreq === 'True') {
                    $(this).addClass('required-val'); //.css('background-color', '#F3D1DC');
                }
            }
        }
        $(this).on('keyup', function () {

            if (this.length === 0) {
                $(check).addClass('required-val'); //.css('background-color', '#F3D1DC');
            } else {
                $(check).removeClass('required-val'); //.css('background-color', '');
            }

        });
    });
}

$(window).on("load", function(){

    var browser = getBrowser();

    $("#clientBrowser").html(browser.name + " " + browser.version);
    $("#clientScreenWidth").html($(window).width());
    $("#clientScreenHeight").html($(window).height());

    if ($.cookie(timezoneCookie) == null) { // if the timezone cookie not exists create one.
 // check if the browser supports cookie
        var testCookie = 'cookie support check';
        $.cookie(testCookie, true);

        if ($.cookie(testCookie)) { // browser supports cookie

            // delete the test cookie.
            $.removeCookie(testCookie);

            // create a new cookie 
            $.cookie(timezoneCookie, new Date().getTimezoneOffset()/*, {path: window.SnitzVars.cookiePath, domain: window.SnitzVars.cookieDomain, expires:30}*/);
            location.reload(); // re-load the page
        }
    }
    else { // if the current timezone and the one stored in cookie are different then
        // store the new timezone in the cookie and refresh the page.

        var storedOffset = parseInt($.cookie(timezoneCookie));
        var currentOffset = new Date().getTimezoneOffset();

        if (storedOffset !== currentOffset) { // user may have changed the timezone
            $.cookie(timezoneCookie, new Date().getTimezoneOffset(),{path: window.SnitzVars.cookiePath, domain: window.SnitzVars.cookieDomain, expires:120});
            //console.log('timezone-reload 2');
            location.reload();
        }
    }
    $('.modal-dialog').draggable({
        handle: ".modal-header"
    });

    $("select[id='theme-change']").on('change',
        function () {
            $(this).tooltip('hide');
            //console.log('theme-reload');
            $.cookie(styleCookieName, encodeURIComponent(this.value), { expires: styleCookieDuration, path: '/' });
            
            location.reload(true);
        });
 
    BootstrapDialog.configDefaultOptions({ animate: false });
    $('.aspinEdit').spinedit();


    //re-enable button if form fails validation
    $(document).on('invalid-form.validate', 'form', function () {
        var button = $(this).find('input[type="submit"]');
        setTimeout(function () {
            button.removeAttr('disabled');
        }, 10);
    });
    //disable button on submit
    $(document).on('submit', 'form', function () {
        var button = $(this).find('input[type="submit"]');
        setTimeout(function () {
            button.attr('disabled', 'disabled');
        }, 1);
    });

    if (isTouchDevice() === false) {
        //tooltip initialisers
        $('.topic-strap').tooltip({
            placement: tooltipPos,
            html: true,
            selector: "a[class=topic-link]"
        });
        $('body').tooltip({
            placement: 'auto',
            selector: '[data-toggle=tooltip]',
            container: 'body'
        });
        $('.lastpost-hint').each(function() {

            $(this).tooltip({
                placement: 'bottom',
                container: 'body',
                title: $(this).attr("tooltip-title")
            });
        });

        $('.modal-link').tooltip();
        $('.gallery-link').tooltip();

    } else {
        $("a").on("click touchend", function (e) {
            var el = $(this);
            var link = el.attr("href");
            window.location = link;
        });
    }

    $('.modal-link').on('click', function (e) {
        e.preventDefault();
        $(this).attr('data-target', '#modal-container');
        $(this).attr('data-toggle', 'modal');

    });
    $('.cancel').on('click', function () {
        window.location.href = document.referrer;
        return false;
    });
    
    //open embedded gallery image in popup window
    $('.view-image').on('click', function (e) {

        e.preventDefault();
        e.stopPropagation();
        e.stopImmediatePropagation();

        var url = $(this).attr('href');
        var params = [
            'height=' + (screen.availHeight-20),
            'width=' + (screen.availWidth-20),
            'scrollbars=yes',
            'resizable=yes'
        ].join(',');
        var popup = window.open(url, 'popupwindow', params);
        popup.moveTo(10, 10);

        return false;
    });
    
    $(document).on('click', 'span.bbc-spoiler', function () {
        var content = $(this).find("span.bbc-spoiler-content");

        if (content.is(":visible")) {
            content.css('display', 'none');
        } else {
            content.css('display', 'block'); 
        }
    });
    $('.confirmed-link').on('click', function (e) {

        e.preventDefault();
        $.ajax({
            type: "POST",
            url: $(this).attr('href'),
            data: { replyid: $(this).val() },
            cache: false,
            success: function(data) {

            }
        });
        });
    $(document).on('click', '#other', function() {
        $( "#emailMemberForm" ).submit();
    });
    $('.send-email').on('click', function (e) {
        e.preventDefault();
        var userid = $(this).data('id');
        $.get(window.SnitzVars.baseUrl + 'Account/EmailMember/' + userid, function (data) {
            $('#emailContainer').html(data);
            $.validator.unobtrusive.parse($("#emailMemberForm"));
            $('#emailModal').modal('show');

        });
    });
    $('.sendpm-link').on('click', function () {
        var username = $(this).data('id');
        $.get(window.SnitzVars.baseUrl + 'PrivateMessage/SendMemberPm/' + username, function (data) {
            $('#pmContainer').html(data);
            $.validator.unobtrusive.parse($("#sendPMForm"));
            $('#modal-sendpm').modal('show');
        });
    });
    $('.sendto-link').on('click', function () {
        var id = $(this).data('id');
        var archive = $.getUrlParam('archived');
        $.get(window.SnitzVars.baseUrl + 'Topic/SendTo/' + id + '?archived=' + archive, function (data) {
            $('#sendToContainer').html(data);
            $.validator.unobtrusive.parse($("#sendToForm"));
            $('#modal-sendto').modal('show');
        });
    });
    $('.showIPAddress').on('click', function () {
        var dilog = BootstrapDialog.show({
            title: 'IP/Domain name',
            message: '<img src="' + window.SnitzVars.baseUrl + 'content/images/link_loader.gif"/>'
        });
        var ip = $(this).data('id');
        $.get(window.SnitzVars.baseUrl + 'Account/ShowIP/?ip=' + ip, function (data) {
            dilog.setMessage(data);
        });
    });
    $('.stopForumSpam').on('click', function(evt) {

        evt.preventDefault();
        var dilog = BootstrapDialog.show({
            title: 'Stop Forum Spam lookup',
            message: '<img src="' + window.SnitzVars.baseUrl + 'content/images/link_loader.gif"/>'
        });
        var id = $(this).data('id');
        var ip = $(this).data('ip');
        var email = $(this).data('email');
        $.get(window.SnitzVars.baseUrl + 'Admin/StopForumSpamCheck/' + id + "/?email=" + email + "&userip=" + ip, function(data) {
            dilog.setMessage(data);
        });
        return false;
    });
    //clear modal cache, so that new content can be loaded
    $('#modal-container').on('hidden.bs.modal', function () {
        $(this).removeData('bs.modal');
    });
    $('.modal').on('hidden.bs.modal', function () {
        $(this).removeData('bs.modal');
    });
    /*update the session topiclist if checkbox selected*/
    $('.topic-select').on('mouseup', function () {
        if (window.console && window.console.log) {
            console.log('toppic-select:' + $(this).val());
            console.log('url:' + window.SnitzVars.baseUrl + "Topic/UpdateTopicList");
        }
        
        $.ajax({
            type: "POST",
            url: window.SnitzVars.baseUrl + "Topic/UpdateTopicList/?id=" + $(this).val(),
            data: { topicid: $(this).val() },
            cache: false
        });
    });
    /*update the session replylist if checkbox selected*/
    $('.reply-select').on('mouseup', function () {

        $.ajax({
            type: "POST",
            url: window.SnitzVars.baseUrl + "Reply/UpdateReplyList",
            data: { replyid: $(this).val() },
            cache: false
        });
    });

    /*remove the sidebox if it's empty*/
    if ($('.side-box').is(':empty') || $.trim($('.side-box').html()).length == 0) {
        $('#right-col').remove();
        $('#main-content').removeClass('col-md-9');
        $('#main-content').removeClass('col-lg-9');

    }
    
    $('#show-about').click(function () {
        var url = $('#aboutModal').data('url');

        $.get(url, function (data) {
            $('#aboutContainer').html(data);

            $('#aboutModal').modal('show');
        });
    });
    $('#show-license').click(function () {
        var url = $('#licenseModal').data('url');

        $.get(url, function (data) {
            $('#licenseContainer').html(data);

            $('#licenseModal').modal('show');
        });
    });
    $('#show-policy').click(function () {
        var url = $('#policyModal').data('url');
        //alert(url);
        $.get(url, function (data) {
            $('#policyContainer').html(data);

            $('#policyModal').modal('show');
        });
    });
    //rotate ad banners
    if ($("#banner-ad-side").length > 0) {
        var b1 = setInterval(function () {
            $.ajax({
                url: window.SnitzVars.baseUrl + "Ad/SideBanner",
                cache: false,
                dataType: "html",
                success: function (data) {
                    $("#banner-ad-side").html(data);
                }
            });
        }, window.SnitzVars.AdDuration * 1000);
    }
    if ($("#banner-ad-top").length > 0) {//"div[id^='banner-ad-top']"
        var b2 = setInterval(function () {
            $.ajax({
                url: window.SnitzVars.baseUrl + "Ad/TopBanner",
                cache: false,
                dataType: "html",
                success: function (data) {
                    $("#banner-ad-top").html(data);
                }
            });
        }, window.SnitzVars.AdDuration * 1000);
    }
    //refresh recent topics
    if ($("#recent-topics").length > 0) {
        var recent = setInterval(function () {
            $("#recent-topics").html("<i class='fa fa-spinner fa-pulse fa-3x fa-fw'></i>");
            $.ajax({
                url: window.SnitzVars.baseUrl + 'Home/RefreshRecentTopics',
                data: { "id": "0" },
                type: "POST",
                cache: false,
                success: function (data) {
                    $("#recent-topics").html(data);
                    ready_or_not();
                }
            });
        }, 1000 * 60 * 5); // 5 min
    }
    //refresh message notify
    if ($("#pm-notify").length > 0) {
        debugger;
        var notify = setInterval(function () {
            if ($("#pm-notify").length > 0) {
                $.ajax({
                    url: window.SnitzVars.baseUrl + 'Home/PMNotify',
                    type: "POST",
                    cache: false,
                    success: function(data) {
                        $("#pm-notify").html(data);
                        ready_or_not();
                    }
                });
            } else { clearInterval(notify);}
        }, 1000 * 60 * 5); // 5 min
    }
    //refresh online-users
    if ($("#online-users").length > 0) {
        var online = setInterval(function () {

            $.ajax({
                url: window.SnitzVars.baseUrl + 'Home/OnlineUsers',
                type: "POST",
                cache: false,
                success: function (data) {
                    $("#online-users").innerHTML = data;
                    ready_or_not();
                }
            });
        }, 1000 * 60 * 3); // 3 min
    }


    /*updates the lastactivity date for members*/
    if (window.SnitzVars.isUserAuthenticated.toLowerCase() === 'true') {
        $.ajax({
            async: true,
            type: "POST",
            url: window.SnitzVars.baseUrl + "Account/UpdateLastActivityDate",
            data: { userName: window.SnitzVars.userName },
            cache: false,
            success: function () { return false; }
        });
    }
    /* Initialise popovers */
    $('.topictitle-hint').popover({
        trigger: 'click',
        container: 'body',
        html: true,
        placement: window.SnitzVars.forumlang === "fa" ? 'left' : 'right',
        "content": function() {
            var div_id = "tmp-id-" + $.now();
            return getPostMessage($(this).data('lastpost'), div_id);
        }
    }); //.on('click', function (e) { e.preventDefault(); $(this).popover('show'); });
    var popOverSettings = {
        placement: 'auto',
        trigger: 'click',
        container: 'body',
        html: true,
        selector: '.lastpost-hint', //Sepcify the selector here
        content: function () {
            var div_id =  "tmp-id-" + $.now();
            return getLastPost($(this).data('lastpost'), div_id);
        }
    }

    $('body').popover(popOverSettings);


    $('.topictitle').on('shown.bs.popover',
        function(e) {
            $(".popover-title").attr("lang", window.SnitzVars.forumlang);
            if (window.SnitzVars.forumlang === "fa") {
                if (!persianRex.rtl.test($(".popover-title").html())) {
                    $(".popover-title").attr("lang", "en");
                }        
            } 
            //setTimeout(function() {
            //        $(e.target).popover('reposition');
            //    },
            //    100);
        });

    $('.lastpost-hint').on('shown.bs.popover',
        function(e) {
            $(".popover-title").attr("lang", window.SnitzVars.forumlang);
            if (window.SnitzVars.forumlang === "fa") {
                if (!persianRex.rtl.test($(".popover-title").html())) {
                    $(".popover-title").attr("lang", "en");
                }        
            } 
            setTimeout(function() {
                    $(e.target).popover('reposition');
                },
                100);
        });
    $('html').on('click', function(e) {
        if (typeof $(e.target).data('original-title') == 'undefined') {
            $('[data-original-title]').popover('hide');
        }
    });
    setInterval(function(){ tick () }, 5000);

    //only enable featured image timer if the
    //featured image div is on the page
    if ($("#fimage").length > 0) {

        $("#featured-img").one("load", function () {
            // do stuff
            $('#featured-img').css('visibility', 'visible').show();
        }).each(function () {
            if (this.complete) $(this).load();
        });
        
        var f = setInterval(function () {
            
            $.ajax({
                url: window.SnitzVars.baseUrl + "PhotoAlbum/FeaturedImage",
                cache: false,
                dataType: "html",
                success: function (data) {
                    $('#featured-img').fadeOut('slow');
                    $("#fimage").html(data);
                    $('#featured-img').fadeIn('slow');
                }
            });
        }, 1000 * 2 * 6); // 2 min 1000 * 2 * 60
    }
});

$(document).ajaxComplete(function () {
   // ready_or_not();
   window.lazyload();
});

function setStyleFromCookie() {
    //console.log('set style');
    var cssTitle = getCookie(styleCookieName);
    //console.log('cookie style' + cssTitle);
    if (typeof cssTitle !== "undefined" && cssTitle !== null && cssTitle.length > 0) {
        //console.log('switch 1');
        switchStyle(cssTitle.toLowerCase());
        if ($("div.theme-changer").is(":visible"))
        {
            //console.log('switch 1 visible');
            $("div.theme-changer select").val(cssTitle);
        }
        
    } else {
        //console.log('switch 2');
        switchStyle(window.SnitzVars.defaultTheme.toLowerCase());
        if ($("div.theme-changer").is(":visible"))
        {
            //console.log('switch 2 visible');
            $("div.theme-changer select").val(window.SnitzVars.defaultTheme.toLowerCase());
        }
        
    }
    
    function getCookie(cookieName) {
        if ($.cookie(cookieName) !== null) {
            var test = $.cookie(cookieName);
            //console.log('???? : ' + test);
            return test;
        }
        //console.log(cookieName + ' not found');
        return '';
    }
}

function ready_or_not() {
    $("blockquote.newquote").attr("lang", window.SnitzVars.forumlang);

    if (window.SnitzVars.forumlang === "fa") {
        window.SnitzVars.useTimeago = "0";
        PersianCheck();
        if (!persianRex.rtl.test($("blockquote.newquote").html())) {
            $("blockquote.newquote").attr("lang", "en");
        }
      
    } else {
        $(".numbers").removeClass("persian");
    }
    
    setRequiredFields();
    if (window.SnitzVars.useTimeago === "1" ) {
        jQuery.timeago.settings.localeTitle = false;
        $("time.timeago").timeago();        
    }
    if (window.SnitzVars.tempScrollTop) {
        $(window).scrollTop(window.SnitzVars.tempScrollTop);
        sessionStorage.removeItem('scrollTop');
    }
    //$("time").hover(function() {
    //    $(this).removeAttr("title");
    //});
}

function isTouchDevice() {

    return navigator.userAgent.match(/iPhone|iPad|iPod/i) != null;

}

var PersianCheck = function() {
    //console.log('persian check');
        $(".numbers").addClass("persian");
        if (!persianRex.hasText.test($(".forum-description").val())) {
            $(".forum-description").attr("dir", "ltr");
        } else {
            $(".forum-description").attr("dir", "rtl");
        }
        if (!persianRex.hasText.test($(".topic-link").val())) {
            $(".subject").attr("dir", "ltr");
            $(".topic-link").attr("dir", "ltr");
        } else {
            $(".subject").attr("dir", "rtl");
            $(".topic-link").attr("dir", "rtl");
        }
        $('.message').each(function() {
            //console.log($(this).html());
            //console.log(persianRex.rtl.test($(this).html()));
            //console.log(persianRex.hasRtl.test($(this).html()));
            if (!persianRex.hasRtl.test($(this).html())) {
                //console.log('use ltr');
                $(this).attr("dir", "ltr");
            } else {
                //console.log('use rtl');
                $(this).attr("dir", "rtl");
            }
        });
        $('.blog-message').each(function(){

            if (!persianRex.hasText.test($(".blog-message").html())) {
                $(".blog-message").attr("dir", "ltr");
            } else {
                $(".blog-message").attr("dir", "rtl");
            }
        });


    $('.numbers').persianNum();  

};

function tick(){
    $('#recent-ticker li:first').slideUp( function () { $(this).appendTo($('#recent-ticker')).slideDown(); });
}

