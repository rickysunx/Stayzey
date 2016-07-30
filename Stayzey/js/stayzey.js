/*
 * Stayzey website javascript
 * Created by: Ricky Sun
 * Date: 15/June/2016
 *
 */

function onSignUp() {
    $("#SignUpDialog").modal({
        backdrop: "static",
        show: true
    });
}

function onLogIn() {
    $("#LoginDialog").modal();
}

function sendRequest(url,data,callback) {
    jQuery.ajax(url, {
        data: data,
        type: "POST",
        success: callback,
        error: function () {
            alert("Network or server internal error!");
        }
    });
}

function submitForm(url, formid, callback) {
    sendRequest(url, $("#" + formid).serialize(), callback);
}

function formShowError(error_info,error_id) {
    if (error_info) {
        $("#"+error_id).removeClass("has-success");
        $("#" + error_id).addClass("has-error");
        $("#" + error_id).popover("destroy");

        setTimeout(function () {
            $("#" + error_id).popover({
                content: error_info,
                trigger: 'hover',
                placement: 'bottom'
            });
        }, 300);
    } else {
        $("#" + error_id).removeClass("has-error");
        $("#" + error_id).addClass("has-success");
        $("#" + error_id).popover("destroy");
    }
}

function submitSignUp() {
    submitForm("/User/SignUp", "form_signup", function (result) {
        if (result.success == 1) {
            alert("You have registered as a member successfully!");
            $("#SignUpDialog").modal("hide");
            
        } else {
            formShowError(result.error_firstname, "fg_signup_firstname");
            formShowError(result.error_lastname, "fg_signup_lastname");
            formShowError(result.error_email, "fg_signup_email");
            formShowError(result.error_password, "fg_signup_password");
            formShowError(result.error_confirmpassword, "fg_signup_confirmpassword");
        }
    });
}

function submitLogin() {
    submitForm("/User/LogIn", "form_login", function (result) {
        if (result.success == 1) {
            $("#LoginDialog").modal("hide");
            window.location.reload();
        } else {
            formShowError(result.error_email, "fg_login_email");
            formShowError(result.error_password, "fg_login_password");
        }
    });
}

function onLogout() {
    sendRequest("/User/LogOut",{}, function () {
        window.location.href = "/";
    });
}

$(document).ready(function () {
    window.signup_dialog_html = $("#SignUpDialog").html();
    window.login_dialog_html = $("#LoginDialog").html();

    $("#SignUpDialog").on("hidden.bs.modal", function () {
        $("#SignUpDialog").html(window.signup_dialog_html);
    });

    $("#LoginDialog").on("hidden.bs.modal", function () {
        $("#LoginDialog").html(window.login_dialog_html);
    });

    var hash = window.location.hash;
    if(hash=="#Login" && (!window.user_login)) {
        $("#LoginDialog").modal();
    }
});


var roomUploadFileId = 1;

function onNewRoomFileUploaded(fileName) {
    roomUploadFileId++;
    var html = "<div id='RoomImage_" + roomUploadFileId + "' class='NewRoomFileListItem'>";
    html += "<input type='hidden' name='RoomImages' value='" + fileName + "'/>";
    html += "<div class='NewRoomFileListItemImage'><img style='width:100px;height:80px;' src='" + fileName + "'/></div>";
    html += "<div class='NewRoomFileListItemRemove'><span title='Delete' onclick='removeRoomImageItem(" + roomUploadFileId + ");' class='glyphicon glyphicon-remove'></span></div>";
    html += "</div>";
    $("#NewRoomFileList").append(html);
}

function onProfileImageUploaded(fileName) {
    $("#profile_image_img").attr("src", fileName);
    $("#profile_image_input").val(fileName);
}

function removeRoomImageItem(id) {
    $("#RoomImage_" + id).remove();
}

function submitRoom() {
    submitForm("/User/SaveRoom", "NewRoomForm", function (result) {
        if (result.success == 1) {
            alert("Room created successfully");
            window.location.href = "/User/Listings";
        } else {
            alert(result.error_info);
        }
    });
}

function onSubmitProfile() {
    submitForm("/User/SaveProfile", "ProfileForm", function (result) {
        if (result.success == 1) {
            alert("Profile saved successfully");
            window.location.reload();
        } else {
            alert(result.error_info);
        }
    });
}

function deleteBooking(id) {
    if (confirm("Are sure to delete this booking?")) {
        sendRequest("/User/DeleteBooking", { id: id }, function (result) {
            if (result.success == 1) {
                window.location.reload();
            } else {
                alert(result.error_info);
            }
        });
    }

}

function cancelBooking(id) {
    sendRequest("/User/CancelBooking", { id: id }, function (result) {
        if (result.success == 1) {
            window.location.reload();
        } else {
            alert(result.error_info);
        }
    });
}

function payBooking(id) {
    sendRequest("/User/PayBooking", { id: id }, function (result) {
        if (result.success == 1) {
            window.location.reload();
        } else {
            alert(result.error_info);
        }
    });
}

function reviewBooking(id) {
    $("#ReviewDialog").html(window._ReviewDialogHTML);
    $("#star_rating_container > div").hover(function () {
        var currentRating = $(this).attr("_rating");
        $("#star_rating_input").val(currentRating);
        for (var i = 1; i <= 5; i++) {
            if (i <= currentRating) {
                $("#star_rating_block_" + i).addClass("star_rating_block_selected");
            } else {
                $("#star_rating_block_" + i).removeClass("star_rating_block_selected");
            }
        }
    });
    $("#review_booking_id").val(id);
    $("#ReviewDialog").modal({
        backdrop: "static",
        show: true
    });
}

function submitReview() {
    submitForm("/User/SaveReview", "form_review", function (result) {
        if (result.success == 1) {
            alert("Your review saved");
            window.location.reload();
        } else {
            alert(result.error_info);
        }
    });
}

function acceptBooking(id) {
    sendRequest("/User/AcceptBooking", { id: id }, function (result) {
        if (result.success == 1) {
            window.location.reload();
        } else {
            alert(result.error_info);
        }
    });
}

function rejectBooking(id) {
    sendRequest("/User/RejectBooking", { id: id }, function (result) {
        if (result.success == 1) {
            window.location.reload();
        } else {
            alert(result.error_info);
        }
    });
}

function unlistRoom(id) {
    sendRequest("/User/ChangeRoomStatus", { id: id,flag: 0 }, function (result) {
        if (result.success == 1) {
            window.location.reload();
        } else {
            alert(result.error_info);
        }
    });
}

function listRoom(id) {
    sendRequest("/User/ChangeRoomStatus", { id: id, flag: 1 }, function (result) {
        if (result.success == 1) {
            window.location.reload();
        } else {
            alert(result.error_info);
        }
    });
}

function deleteRoom(id) {
    if (confirm("Are you sure to delete this room?")) {
        sendRequest("/User/DeleteRoom", { id: id }, function (result) {
            if (result.success == 1) {
                window.location.reload();
            } else {
                alert(result.error_info);
            }
        });
    }
}




