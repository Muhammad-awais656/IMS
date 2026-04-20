// Common changes
var dataUrl = null;
var tableId = null;

function RenderHomePage() {
    window.open("../Home/Index", "_self");
}

function Finance() {
    
    $.get('../api/Finance/Authorize', function (data) {
        
        RedirectToUrl(data.redirect_url);
    });

}

var defaultExportAction = function (self, e, dt, button, config) {
    
    if (button[0].className.indexOf('buttons-excel') >= 0) {
        if ($.fn.dataTable.ext.buttons.excelHtml5.available(dt, config)) {
            $.fn.dataTable.ext.buttons.excelHtml5.action.call(self, e, dt, button, config);
        }
        else {
            $.fn.dataTable.ext.buttons.excelFlash.action.call(self, e, dt, button, config);
        }
    } else if (button[0].className.indexOf('buttons-pdf') >= 0) {
        if ($.fn.dataTable.ext.buttons.pdfHtml5.available(dt, config)) {
            $.fn.dataTable.ext.buttons.pdfHtml5.action.call(self, e, dt, button, config);
        }
        else {
            $.fn.dataTable.ext.buttons.pdfFlash.action.call(self, e, dt, button, config);
        }
    }
    else if (button[0].className.indexOf('buttons-csv') >= 0) {
        
        if ($.fn.dataTable.ext.buttons.csvHtml5.available(dt, config)) {
            $.fn.dataTable.ext.buttons.csvHtml5.action.call(self, e, dt, button, config);
        }
        else {
            $.fn.dataTable.ext.buttons.csvFlash.action.call(self, e, dt, button, config);
        }
    }
    else if (button[0].className.indexOf('buttons-print') >= 0) {
        $.fn.dataTable.ext.buttons.print.action.call(self, e, dt, button, config);
    }
    else if (button[0].className.indexOf('buttons-print') >= 0) {
        $.fn.dataTable.ext.buttons.print.action(e, dt, button, config);
    }
};

var customExportAction = function (e, dt, button, config) {
    
    var self = this;
    var oldStart = dt.settings()[0]._iDisplayStart;

    dt.one('preXhr', function (e, s, data) {
        // Just this once, load all data from the server...
        data.start = 0;
        data.length = 2147483647;
        
        dt.one('preDraw', function (e, settings) {
            // Call the original action function 
            defaultExportAction(self, e, dt, button, config);

            dt.one('preXhr', function (e, s, data) {
                // DataTables thinks the first item displayed is index 0, but we're not drawing that.
                // Set the property to what it was before exporting.
                settings._iDisplayStart = oldStart;
                data.start = oldStart;
            });

            // Reload the grid with the original page. Otherwise, API functions like table.cell(this) don't work properly.
            setTimeout(dt.ajax.reload, 0);

            // Prevent rendering of the full data to the DOM
            return false;
        });
    });

    // Requery the server with the new one-time export settings
    dt.ajax.reload();
};

function RevokeNegative(obj) {
    
    var inputValue = obj.value;
    var intValue = parseInt(inputValue);

    if (isNaN(intValue) || intValue < 1 || intValue > 100) {


        $("#" + obj.id).val('');
    }
}
function OnlyPositiveIntegers(obj) {
    
    var inputValue = obj.value;
    var intValue = parseInt(inputValue);

    if (isNaN(intValue) || intValue < 1 ) {


        $("#" + obj.id).val('');
    }
}

//NOt Allow First Space

$(".nospace").keypress(function (e) {

    if (this.value.length === 0 && e.which === 32) e.preventDefault();
});

function checkFirstLetterSpace(_string) {
    if (_string.charCodeAt(0) === 32) {
        return true;
    }
    else return false;
}

//For Image PATH Conversion
function absolutePath(href) {
    var link = document.createElement("a");
    link.href = href;
    return link.href;
}
$(".AllowPositiveandDecimalIntegers").keydown(function (event) {

    if (event.shiftKey) {
        event.preventDefault();
    }

    if (event.keyCode == 46 || event.keyCode == 8 || event.keyCode == 190 || event.keyCode == 9 || event.keyCode == 110) {

    }
    else {
        if (event.keyCode < 95) {
            if (event.keyCode < 48 || event.keyCode > 57) {
                event.preventDefault();
            }
        }
        else {
            if (event.keyCode < 96 || event.keyCode > 105) {
                event.preventDefault();
            }
        }
    }
});
$(".AllowPositiveIntegers").keydown(function (event) {

    if (event.shiftKey) {
        event.preventDefault();
    }

    if (event.keyCode == 46 || event.keyCode == 8 || event.keyCode == 9) {

    }
    else {
        if (event.keyCode < 95) {
            if (event.keyCode < 48 || event.keyCode > 57) {
                event.preventDefault();
            }
        }
        else {
            if (event.keyCode < 96 || event.keyCode > 105) {
                event.preventDefault();
            }
        }
    }
});

function IsNullOrEmpty(value) {
    return typeof value == 'string' && !value.trim() || typeof value == 'undefined' || value === null;
}
function ShowLoader() {
    $("#contentwrapperdiv").append('<div id="lloading"><div id="div-loading" class="loader">Loading<span></span></div></div>');
}
function HideLoader() {
    let loader = $("#contentwrapperdiv").find("#div-loading");
    let loaderbg = $("#contentwrapperdiv").find("#lloading");
    loaderbg.remove();
    loader.remove();
}
function ShowNewLoader() {
    $("#contentwrapperdiv").append('<div id="ndiv-loading" class="lds-hourglass"></div>');
}
function HideNewLoader() {
    let loader = $("#contentwrapperdiv").find("#ndiv-loading");
    let loaderbg = $("#contentwrapperdiv").find("#lloading");
    loaderbg.remove();
    loader.remove();
}
function loadInitialPlotDataAgainstSociety(id) {
    console.log('SocietyID: ' + id);
    if (id == "") {

        UnselectKendoDropDown("phaseext");
        LoadKendoDropDown('phaseext', '-- Please Select Phase/Extension --', '');

        LoadKendoDropDown('societyblock', '-- Please Select Block/Sector --', '');

    }
    else {

        LoadKendoDropDown('phaseext', '-- Please Select Phase/Extension --', '../api/PhaseExtension/GetActivePhaseExtensionsBySociety?id=' + id);

        LoadKendoDropDown('societyblock', '-- Please Select Block/Sector --', '../api/CommonValues/GetAllSocietyBlocksBySocietyId?id=' + id);
        LoadKendoDropDown('societybuildings', '-- Please Select Society Buildings --', '../api/CommonValues/GetAllSocietyBuildingsBySocietyId?id=' + id);

    }

    $.get('../api/CommonValues/GetSocietyAreaScaleBySocietyId?id=' + id, function (data) {

        if (data.length == 0) {
            $("#Skanal").val("0");
            $("#SMarla").val("0");
        }
        else {
            $("#Skanal").val(data[0].Marlainkanal);
            $("#SMarla").val(data[0].SqftsinMarla);
        }
    });

}

function loadInitialPlotDataAgainstSocietyforr(id) {
    // 
    if (id == "") {

        UnselectKendoDropDown("rphaseext");
        LoadKendoDropDown('rphaseext', '-- Please Select Phase/Extension --', '');

        LoadKendoDropDown('rsocietyblock', '-- Please Select Block/Sector --', '');

    }
    else {

        LoadKendoDropDown('rphaseext', '-- Please Select Phase/Extension --', '../api/PhaseExtension/GetActivePhaseExtensionsBySociety?id=' + id);

        LoadKendoDropDown('rsocietyblock', '-- Please Select Block/Sector --', '../api/CommonValues/GetAllSocietyBlocksBySocietyId?id=' + id);
        LoadKendoDropDown('rsocietybuildings', '-- Please Select Society Buildings --', '../api/CommonValues/GetAllSocietyBuildingsBySocietyId?id=' + id);

    }

    $.get('../api/CommonValues/GetSocietyAreaScaleBySocietyId?id=' + id, function (data) {

        if (data.length == 0) {
            $("#Skanal").val("0");
            $("#SMarla").val("0");
        }
        else {
            $("#Skanal").val(data[0].Marlainkanal);
            $("#SMarla").val(data[0].SqftsinMarla);
        }
    });

}
function loadPhasesAgainstSociety(id) {

    if (id == "") {

        UnselectKendoDropDown("phaseext");
        LoadKendoDropDown('phaseext', '-- Please Select Phase/Extension --', '');



    }
    else {

        LoadKendoDropDown('phaseext', '-- Please Select Phase/Extension --', '../api/PhaseExtension/GetActivePhaseExtensionsBySociety?id=' + id);



    }



}


function loadPhasesAgainstSocietyAndAreaScale(id) {
    
    if (id == "") {

        UnselectKendoDropDown("phaseext");
        LoadKendoDropDown('phaseext', '-- Please Select Phase/Extension --', '');



    }
    else {

        LoadKendoDropDown('phaseext', '-- Please Select Phase/Extension --', '../api/PhaseExtension/GetActivePhaseExtensionsBySociety?id=' + id);

        $.get('../api/CommonValues/GetSocietyAreaScaleBySocietyId?id=' + id, function (data) {
            
            if (data.length == 0) {
                $("#Skanal").val("0");
                $("#SMarla").val("0");
            }
            else {
                $("#Skanal").val(data[0].Marlainkanal);
                $("#SMarla").val(data[0].SqftsinMarla);
            }
        });

    }



}

function LoadBlockSectorByPhase(id) {


    if (id == "") {

        UnselectKendoDropDown("phaseext");

        LoadKendoDropDown('phaseext', '-- Please Select Phase/Extension --', '');
        LoadKendoDropDown('societyblock', '-- Please Select Block/Sector --', '');
    }
    else {
        var sid = $("#societies").val();
        if (sid != "") {
            LoadKendoDropDown('societyblock', '-- Please Select Block/Sector --', '../api/CommonValues/GetBlocksByPhaseandSociety?id=' + id + '&sid=' + sid);
        }
        else {
            toastr.warning("Please select Society first", { timeOut: 4000 });

        }

    }



}
function LoadBlockSectorByPhaseforr(id) {


    if (id == "") {

        UnselectKendoDropDown("rphaseext");

        LoadKendoDropDown('rphaseext', '-- Please Select Phase/Extension --', '');
        LoadKendoDropDown('rsocietyblock', '-- Please Select Block/Sector --', '');
    }
    else {
        var sid = $("#rsocieties").val();
        if (sid != "") {
            LoadKendoDropDown('rsocietyblock', '-- Please Select Block/Sector --', '../api/CommonValues/GetBlocksByPhaseandSociety?id=' + id + '&sid=' + sid);
        }
        else {
            toastr.warning("Please select Society first", { timeOut: 4000 });

        }

    }



}

function GetPromise(apiUrl) {
    return new Promise((resolve, reject) => {
        //debugger
        $.ajax({
            url: apiUrl,
            method: 'GET'
        }).done((response) => {
            //this means my api call suceeded, so I will call resolve on the response
            resolve(response);
        }).fail((error) => {
            //this means the api call failed, so I will call reject on the error
            reject(error);
        });
    });
};

function GetUserImage() {
    $.ajax({
        url: "../api/Employee/GetEmployeeImage",
        type: "GET",
        contentType: "application/json;charset=utf-8",
        success: function (data) {
            if (data === "")
                $('#user-image').attr("src", "../Content/assets/img/profiles/avatar-21.jpg");
            else
                $('#user-image').attr("src", "../ImagesStorageDB/" + data);
        },

        error: function (data) {
            var response = data.responseText.replace(/"/g, '');
            console.log(response);
        }
    });


};
function GetNotifications() {

    $.ajax({
        url: "../api/Notification/GetNotification",
        type: "GET",
        contentType: "application/json;charset=utf-8",
        success: function (data) {

            if (data.Notifications.length >= 1) {

                for (var i = 0; i < data.Notifications.length; i++) {

                    $('#notificationBar').append('<li class="notification-message">' +
                        '<a href="../NotificationManagement/Index">' +
                        '<div class="media">' +
                        '<div class="media-body">' +
                        '<p class="text-dark"><span></span><b>' + data.Notifications[i].Name + ': </b><span class="noti-title">' + data.Notifications[i].Description + '</span></p>' +
                        '<p class="noti-time"><span class="notification-time">' + data.Notifications[i].TotalTime + '</span></p>' +
                        '</div>' +
                        '</div>' +
                        '</a>' +
                        '</li>');


                }
            }
            $("#notificationCount").text(data.Count)

            HideLoader();
        },

        error: function (data) {
            var response = data.responseText.replace(/"/g, '');
            $("#success").hide();
            $("#error").html(response);
            $("#error").show();
            HideLoader();
        }
    });


};

function loadSocietyBuildings(id) {

    if (id != "") {
        LoadKendoDropDown('societybuildings', '-- Please Select Society Buildings --', '../api/CommonValues/GetAllSocietyBuildingsBySocietyId?id=' + id);

    }

}
function loadSocietyBuildingsByBlockSector(id) {

    if (id != "") {
        LoadKendoDropDown('societybuildings', '-- Please Select Society Buildings --', '../api/CommonValues/loadSocietyBuildingsByBlockSector?id=' + id);

    }

}
function loadSocietyBuildingFloors(id) {

    if (id != "") {
        LoadKendoDropDown('buildingfloor', '-- Please Select Society Buildings Floor --', '../api/CommonValues/GetAllSocietyBuildingFloorsByBuildingId?id=' + id);

    }

}
function loadFLoorLandUse(id) {

    if (id != "") {
        LoadKendoDropDown('floorlandUse', '-- Please Select Land Use --', '../api/CommonValues/GetSocietyLandUseFloorbyid?id=' + id);

    }

}
function loadFLoorLandUseByFLoorAndBuildingId(id) {
    var BuildingId = parseInt($("#societybuildings").val());    
    if (id != "") {
        LoadKendoDropDown('floorlandUse', '-- Please Select Land Use --', '../api/CommonValues/GetSocietyLandUseFLoorAndBuildingId?id=' + id + '&BuildingId=' + BuildingId);

    }

}
function LoadKendoDropDown(id, placeholder, url) {

    var promiseObject = GetPromise(url);
    promiseObject
        .then(data =>
            SetKendoDDSource(id, placeholder, data))
        .catch(error =>
            console.log("KENDO DROPDOWN ERROR" + "/r/n" + "Url:" + url + "/r/n" + "htmlElementId:" + id + "/r/n" + "Error Detail" + error));
};




function LoadKendoGrid(id, placeholder, url) {

    var promiseObject = GetPromise(url);
    promiseObject
        .then(data =>
            SetKendoGridDDSource(id, placeholder, data))
        .catch(error =>
            console.log("KENDO DROPDOWN ERROR" + "/r/n" + "Url:" + url + "/r/n" + "htmlElementId:" + id + "/r/n" + "Error Detail" + error));
};

function LoadTehsilKendoDropDown(id, placeholder, url) {
    var promiseObject = GetPromise(url);
    promiseObject
        .then(data =>
            SetTehsilKendoDDSource(id, placeholder, data))
        .catch(error =>
            console.log("KENDO DROPDOWN ERROR" + "/r/n" + "Url:" + url + "/r/n" + "htmlElementId:" + id + "/r/n" + "Error Detail" + error));
};
function LoadCountryKendoDropDown(id, placeholder, url) {
    var promiseObject = GetPromise(url);
    promiseObject
        .then(data =>
            SetCountrylKendoDDSource(id, placeholder, data))
        .catch(error =>
            console.log("KENDO DROPDOWN ERROR" + "/r/n" + "Url:" + url + "/r/n" + "htmlElementId:" + id + "/r/n" + "Error Detail" + error));
};
function LoadDistrictKendoDropDown(id, placeholder, url) {
    var promiseObject = GetPromise(url);
    promiseObject
        .then(data =>
            SetDistrictKendoDDSource(id, placeholder, data))
        .catch(error =>
            console.log("KENDO DROPDOWN ERROR" + "/r/n" + "Url:" + url + "/r/n" + "htmlElementId:" + id + "/r/n" + "Error Detail" + error));
};
function LoadDivisionKendoDropDown(id, placeholder, url) {
    var promiseObject = GetPromise(url);
    promiseObject
        .then(data =>
            SetDivisionKendoDDSource(id, placeholder, data))
        .catch(error =>
            console.log("KENDO DROPDOWN ERROR" + "/r/n" + "Url:" + url + "/r/n" + "htmlElementId:" + id + "/r/n" + "Error Detail" + error));
};
function LoadSocitiesKendoDropDown(id, placeholder, url) {
    var promiseObject = GetPromise(url);
    promiseObject
        .then(data =>
            SetSocitiesKendoDDSource(id, placeholder, data))
        .catch(error =>
            console.log("KENDO DROPDOWN ERROR" + "/r/n" + "Url:" + url + "/r/n" + "htmlElementId:" + id + "/r/n" + "Error Detail" + error));
};

function LoadKendoMultiselect(id, placeholder, url, callbackOrInitialValues) {
    var initialValues = Array.isArray(callbackOrInitialValues) ? callbackOrInitialValues : null;
    var callback = typeof callbackOrInitialValues === 'function' ? callbackOrInitialValues : null;
    var promiseObject = GetPromise(url);
    return promiseObject
        .then(function (data) {
            SetKendoMultiSource(id, placeholder, data);
            if (initialValues && initialValues.length > 0) {
                var w = $("#" + id).data("kendoMultiSelect");
                if (w) w.value(initialValues);
            }
            if (callback) callback();
            return $("#" + id).data("kendoMultiSelect");
        })
        .catch(function (error) {
            console.log("KENDO MULTISELECT ERROR" + "/r/n" + "Url:" + url + "/r/n" + "htmlElementId:" + id + "/r/n" + "Error Detail" + error);
        });
};


function LoadKendoAutoComplete(id, placeholder, url) {
    var promiseObject = GetPromise(url);
    promiseObject
        .then(data =>
            SetKendoAutoCompleteSource(id, placeholder, data))
        .catch(error =>
            console.log("KENDO AUTO COMPLETE ERROR" + "/r/n" + "Url:" + url + "/r/n" + "htmlElementId:" + id + "/r/n" + "Error Detail" + error));
};

function SetKendoGridDDSource(id, placeholder, data) {

    $("#" + id).kendoComboBox({
        dataTextField: "SocietyName",
        dataValueField: "SocietyOID",
        optionLabel: placeholder,
        filter: "contains",
        autoBind: false,
        minLength: 3,
        height: 200,
        serverFiltering: true,
        dataSource: data

    });
};

function SetKendoDDSource(id, placeholder, data) {
    
    try {
        var dropdownlist = $("#" + id).data("kendoDropDownList");
        dropdownlist.destroy();

    } catch (e) {

    }

    $("#" + id).kendoDropDownList({

        dataTextField: "Name",
        dataValueField: "Id",
        optionLabel: placeholder,
        dataSource: data,
        sort: { field: "Name", dir: "asc" },
        filter: "contains",
        height: 200,
        popup: {
            appendTo: $("#" + id + "modal")
        }
    });


};

function SetDivisionKendoDDSource(id, placeholder, data) {
    try {
        var dropdownlist = $("#" + id).data("kendoDropDownList");
        dropdownlist.destroy();

    } catch (e) {

    }
    $("#" + id).kendoDropDownList({
        dataTextField: "div_name",
        dataValueField: "div_id",
        optionLabel: placeholder,
        dataSource: data,
        filter: "contains",
        height: 200,
        popup: {
            appendTo: $("#" + id + "modal")
        }
    });
};
function SetSocitiesKendoDDSource(id, placeholder, data) {
    try {
        var dropdownlist = $("#" + id).data("kendoDropDownList");
        dropdownlist.destroy();

    } catch (e) {

    }
    $("#" + id).kendoDropDownList({
        dataTextField: "SocietyName",
        dataValueField: "SocietyOID",
        optionLabel: placeholder,
        dataSource: data,
        filter: "contains",
        height: 200


    });
};
function SetDistrictKendoDDSource(id, placeholder, data) {
    try {
        var dropdownlist = $("#" + id).data("kendoDropDownList");
        dropdownlist.destroy();

    } catch (e) {

    }
    $("#" + id).kendoDropDownList({
        dataTextField: "dist_name",
        dataValueField: "dist_id",
        optionLabel: placeholder,
        dataSource: data,
        filter: "contains",
        height: 200,
        popup: {
            appendTo: $("#" + id + "modal")
        }
    });
};
function SetTehsilKendoDDSource(id, placeholder, data) {

    try {
        var dropdownlist = $("#" + id).data("kendoDropDownList");
        dropdownlist.destroy();

    } catch (e) {

    }

    $("#" + id).kendoDropDownList({
        dataTextField: "teh_name",
        dataValueField: "teh_id",
        optionLabel: placeholder,
        dataSource: data,
        filter: "contains",
        height: 200,
        popup: {
            appendTo: $("#" + id + "modal")
        }
    });
};
function SetCountrylKendoDDSource(id, placeholder, data) {

    try {
        var dropdownlist = $("#" + id).data("kendoDropDownList");
        dropdownlist.destroy();

    } catch (e) {

    }

    $("#" + id).kendoDropDownList({
        dataTextField: "country_name",
        dataValueField: "country_id",
        optionLabel: placeholder,
        dataSource: data,
        filter: "contains",
        height: 200,
        popup: {
            appendTo: $("#" + id + "modal")
        }
    });
};

function SetKendoDDSourceValue(id, placeholder, data, value) {
    $("#" + id).kendoDropDownList({
        dataTextField: "Name",
        dataValueField: "Id",
        optionLabel: placeholder,
        dataSource: data,
        height: 100
    });

    if (value != null) {
        $("#" + id).val(value);
    }
};

function SetKendoMultiSource(id, placeholder, data) {
    // ASP.NET Core JSON uses camelCase by default (id, name)
    var dataTextField = (data && data[0]) ? (data[0].Name !== undefined ? "Name" : "name") : "name";
    var dataValueField = (data && data[0]) ? (data[0].Id !== undefined ? "Id" : "id") : "id";
    $("#" + id).kendoMultiSelect({
        dataTextField: dataTextField,
        dataValueField: dataValueField,
        optionLabel: placeholder,
        placeholder: placeholder,
        dataSource: data,
        height: 100
    });
};

function SetKendoAutoCompleteSource(id, placeholder, data) {
    $("#" + id).kendoAutoComplete({
        dataTextField: "Name",
        dataValueField: "Id",
        suggest: true,
        dataSource: data,
        filter: "contains",
        placeholder: placeholder,
        select: onSelectRecord,
        change: OnChangeRecord
    });
};
function onSelectRecord(e) {

    var inputId = this.element[0].id;
    var dataItem = this.dataItem(e.item.index());

    $("#" + inputId + "Id").val(dataItem.Id);
    $("#" + inputId + "NameOnSelect").val(dataItem.Name);
}

function OnChangeRecord(e) {

    var inputId = this.element[0].id;
    var newName = $("#" + inputId).val();
    var oldName = $("#" + inputId + "NameOnSelect").val();
    if (newName != oldName) {
        $("#" + inputId + "Id").val("");
    }
}


function InitializeKendoDatePicker(id, disableFutureDates, disablePastDates) {
    $("#" + id).kendoDatePicker({
        value: new Date(),
        disableDates: function (date) {
            if (disableFutureDates) {
                return date > new Date();
            }
            else if (disablePastDates) {
                var oldDate = new Date();
                oldDate.setDate(oldDate.getDate() - 1);
                return date < oldDate;
            }
        }
    });
};

function LoadDropDown(id, url, value, placeholder) {
    var promiseObject = GetPromise(url);
    promiseObject
        .then(data =>
            SetKendoDDSourceValue(id, placeholder, data, value))
        .catch(error => console.log("DROPDOWN ERROR" + "/r/n" + "Url:" + url + "/r/n" + "htmlElementId:" + id + "/r/n" + "Error Detail" + error));
};




function UnselectKendoDropDown(id) {
    var ddList = $('#' + id).data("kendoDropDownList");
    if (ddList != undefined) {
        ddList.text(ddList.options.optionLabel);
        ddList.element.val(-1);
        ddList.selectedIndex = -1;
        ddList._oldIndex = 0;
    }
};

function ClearKendoDropDownDataSource(id) {
    UnselectKendoDropDown(id);
    var ddList = $('#' + id).data("kendoDropDownList");
    if (ddList != undefined) {
        ddList.dataSource.data([]);
    }
};

function GetgridValidFlagDescription(ValidFlag, rowId, dtCommonParam) {

    var flagStatus, styleClasses;
    if (ValidFlag) {
        flagStatus = " Active"
        styleClasses = "fa fa-dot-circle-o text-success"
    }
    else {
        flagStatus = " Inactive"
        styleClasses = "fa " + "fa-dot-circle-o " + "text-danger";
    }

    if (flagStatus == " Active") {


        var actionHtml = '<div class="dropdown action-label">' +
            '<a class="btn btn-white btn-sm btn-rounded dropdown-toggle" href="#" data-toggle="dropdown" aria-expanded="false">' +
            '<i class=' + '"' + styleClasses + '"' + '></i>' + flagStatus +
            '</a>' +
            '<div class="dropdown-menu dropdown-menu-right">' +

            '<a class="dropdown-item" href="#" onclick ="ShowModal(' + "'" + dtCommonParam.deActivateUrl + rowId + "'," + "'" + dtCommonParam.tableId + "'," + "'" + "inactive'" + ')"><i class="fa fa-dot-circle-o text-danger"></i> Inactive</a>' +
            '</div>' +
            '</div>';
        return actionHtml;
    }
    else {
        var actionHtml = '<div class="dropdown action-label">' +
            '<a class="btn btn-white btn-sm btn-rounded dropdown-toggle" href="#" data-toggle="dropdown" aria-expanded="false">' +
            '<i class=' + '"' + styleClasses + '"' + '></i>' + flagStatus +
            '</a>' +
            '<div class="dropdown-menu dropdown-menu-right">' +
            '<a class="dropdown-item" href="#" onclick ="ShowModal(' + "'" + dtCommonParam.activateUrl + rowId + "'," + "'" + dtCommonParam.tableId + "'," + "'" + "active'" + ')"' + '><i class="fa fa-dot-circle-o text-success"></i> active</a>' +

            '</div>' +
            '</div>';
        return actionHtml;
    }
};

function GetnewgridValidFlagDescription(ValidFlag, rowId, dtCommonParam) {

    var flagStatus, styleClasses;
    if (ValidFlag) {
        flagStatus = " active"

        var switchbtn = '<div onclick ="ShowModal(' + "'" + dtCommonParam.deActivateUrl + rowId + "'," + "'" + dtCommonParam.tableId + "'," + "'" + "inactive'" + ')" class="cust-check active"><span></span></div>';
        return switchbtn;
    }
    else {
        flagStatus = ""
        var switchbtn = '<div onclick ="ShowModal(' + "'" + dtCommonParam.activateUrl + rowId + "'," + "'" + dtCommonParam.tableId + "'," + "'" + "active'" + ')" class="cust-check"><span></span></div>';
        return switchbtn;
    }

    //var switchbtn = '<div onclick="ValidflagModal(' + "'" + dtCommonParam.activateUrl + rowId + "'," + "'" + dtCommonParam.tableId + "'," + "'" + "'" + dtCommonParam.deActivateUrl + "''" + ')" class="cust-check ' + flagStatus + '"><span></span></div>';
    //return switchbtn;
    //var html ='<div  style="z-index:100">'+
    //    '<div class="form-check form-switch" >' +
    //   '<input class="form-check-input" style="z-index:100" type="checkbox" role="switch" onchange="Validflag()"  id="flexSwitchCheckChecked" ' + flagStatus +' >' +
    //    '</div>'+
    //    '</div>';

    //return html;
    //var actionHtml = '<div class="dropdown action-label">' +
    //    '<a class="btn btn-white btn-sm btn-rounded dropdown-toggle" href="#" data-toggle="dropdown" aria-expanded="false">' +
    //    '<i class=' + '"' + styleClasses + '"' + '></i>' + flagStatus +
    //    '</a>' +
    //    '<div class="dropdown-menu dropdown-menu-right">' +
    //    '<a class="dropdown-item" href="#" onclick ="ShowModal(' + "'" + dtCommonParam.activateUrl + rowId + "'," + "'" + dtCommonParam.tableId + "'," + "'" + "active'" + ')"' + '><i class="fa fa-dot-circle-o text-success"></i> active</a>' +
    //    '<a class="dropdown-item" href="#" onclick ="ShowModal(' + "'" + dtCommonParam.deActivateUrl + rowId + "'," + "'" + dtCommonParam.tableId + "'," + "'" + "inactive'" + ')"><i class="fa fa-dot-circle-o text-danger"></i> Inactive</a>' +
    //    '</div>' +
    //    '</div>';
    //return actionHtml;
};

function RedirectToUrl(url) {
    location.href = url;
};

function Validflag() {

    var status = false;
}
function ReloadDataTable(id) {

    $('#' + id).DataTable().ajax.reload();
};
function toggleDropdown(e, rowId) {
    e.preventDefault();
    $(`#dropdown${rowId}`).toggleClass("show");
}
function ActivateRecord() {
    //ShowModal("inactive")
    //var ans = confirm("Are you sure you want to Activate this record?");
    $("#active").modal("hide");
    var ans = true;
    if (ans) {
        //ShowLoader();
        $.ajax({
            url: dataUrl,
            type: "POST",
            contentType: "application/json;charset=UTF-8",
            //dataType: "json",
            data: "{}",
            success: function (result) {
                //HideLoader();
                ReloadDataTable(tableId);
            },
            error: function (errormessage) {
                //HideLoader();
                toastr.warning(errormessage.responseText, { timeout: 5000 });
            }
        });

    }
};

function DeactivateRecord() {
    //var ans = confirm("Are you sure you want to Deactivate this record?");
    //ShowModal("active")
    $("#inactive").modal("hide");
    var ans = true;
    if (ans) {
        //ShowLoader();
        $.ajax({
            url: dataUrl,
            type: "POST",
            contentType: "application/json;charset=UTF-8",
            // dataType: "json",
            data: "{}",
            success: function (result) {

                if (tableId != undefined && tableId != null) {
                    ReloadDataTable(tableId);
                }
                else {
                    FetchEventAndRenderCalendar();
                }
                HideLoader();
            },
            error: function (errormessage) {
                //HideLoader();
                toastr.warning(errormessage.responseText, { timeout: 5000 });
            }
        });

    }
};

function CancelById() {
    $("#cancelModel").modal("hide");
    var ans = true;
    if (ans) {
        //ShowLoader();
        $.ajax({
            url: dataUrl,
            type: "POST",
            contentType: "application/json;charset=UTF-8",
            //dataType: "json",
            data: "{}",
            success: function (result) {
                //HideLoader();
                ReloadDataTable(tableId);
            },
            error: function (errormessage) {
                //HideLoader();
                alert(errormessage.responseText);
            }
        });

    }
};

function RejectedRecord() {
    //ShowModal("inactive")
    //var ans = confirm("Are you sure you want to Activate this record?");
    $("#Rejected").modal("hide");
    var ans = true;
    if (ans) {
        //ShowLoader();
        $.ajax({
            url: dataUrl,
            type: "POST",
            contentType: "application/json;charset=UTF-8",
            //dataType: "json",
            data: "{}",
            success: function (result) {
                //HideLoader();
                ReloadDataTable(tableId);
            },
            error: function (errormessage) {
                //HideLoader();
                alert(errormessage.responseText);
            }
        });

    }
};

function ApprovedRecord() {
    //ShowModal("inactive")
    //var ans = confirm("Are you sure you want to Activate this record?");
    $("#Approved").modal("hide");
    var ans = true;
    if (ans) {
        //ShowLoader();
        $.ajax({
            url: dataUrl,
            type: "POST",
            contentType: "application/json;charset=UTF-8",
            //dataType: "json",
            data: "{}",
            success: function (result) {
                //HideLoader();
                ReloadDataTable(tableId);
            },
            error: function (errormessage) {
                //HideLoader();
                alert(errormessage.responseText);
            }
        });

    }
};

function CloseRecord() {
    //ShowModal("inactive")
    //var ans = confirm("Are you sure you want to Activate this record?");
    $("#Closed").modal("hide");
    var ans = true;
    if (ans) {
        //ShowLoader();
        $.ajax({
            url: dataUrl,
            type: "POST",
            contentType: "application/json;charset=UTF-8",
            //dataType: "json",
            data: "{}",
            success: function (result) {
                //HideLoader();
                ReloadDataTable(tableId);
            },
            error: function (errormessage) {
                //HideLoader();
                alert(errormessage.responseText);
            }
        });

    }
};

function GetUpdateAction(rowId, dtCommonParam,) {
    var actionsHtml = '<div>' +
        (dtCommonParam.updateUrl ?
            ('<a href=" ' + dtCommonParam.updateUrl + rowId + ' " class="tooltip-success">' +
                '<span class="waves-light"><li class="ace-icon  fa fa-pencil-square-o bigger-120 font12" title="Edit" style="margin-right: 5px;"></li></span>' +
                '</a > ') : '') +

        '</div>';
    return actionsHtml;
}
function GetnewUpdateAction(rowId, dtCommonParam,) {
    var actionsHtml = '<div>' +
        (dtCommonParam.updateUrl ?
            ('<a href=" ' + dtCommonParam.updateUrl + rowId + ' " class="btn btn-action">' +
                '<i class="fa-solid fa-eye"></i>' +
                '</a > ') : '') +

        '</div>';
    return actionsHtml;
}
//function GetLayoutPlanActions(rowId, dtCommonParam,) {
//    var actionsHtml = '<div>' +
//        (dtCommonParam.updateUrl ?
//            ('<a href=" ' + dtCommonParam.updateUrl + rowId + ' " class="btn-success btn-sm">' +
//                '<span class="waves-light"><li class="ace-icon  fa fa-pencil-square-o bigger-120 font12" data-toggle="tooltip" title="Edit" style="margin-top: 15px;"></li></span>' +
//                '</a > ') : '') +
//        (dtCommonParam.AddLandDistributionUrl ?
//            ('<a   href=" ' + dtCommonParam.AddLandDistributionUrl + rowId + ' " class="btn-success btn-sm" ">' +
//                '<span class="waves-light"><li class="ace-icon  fa fa-plus bigger-120 font12" data-toggle="tooltip" title="Add Land Distribution" style="margin-top: 15px;"></li></span>' +
//                '</a > ') : '') +

//        (dtCommonParam.DetailsUrl ?
//            ('<a href=" ' + dtCommonParam.DetailsUrl + rowId + ' " class="btn-success btn-sm">' +
//                '<span class="waves-light"><li class="ace-icon  fa fa-eye bigger-120 font12" data-toggle="tooltip" title="Detail" style="margin-top: 15px;"></li></span>' +
//                '</a > ') : '') +

//        '</div>';
//    return actionsHtml;
//}



function GetPlotActions(rowId, dtCommonParam,) {
    var actionsHtml = '<div>' +
        (dtCommonParam.updateUrl ?
            ('<a href=" ' + dtCommonParam.updateUrl + rowId + ' " class="btn-success btn-sm">' +
                '<span class="waves-light"><li class="ace-icon  fa fa-pencil-square-o bigger-120 font12" data-toggle="tooltip" title="Edit" style="margin-top: 15px;"></li></span>' +
                '</a > ') : '') +
        (dtCommonParam.AllotMemberUrl ?
            ('<a   href=" ' + dtCommonParam.AllotMemberUrl + rowId + ' " class="btn-success btn-sm" ">' +
                '<span class="waves-light"><li class="ace-icon  fa fa-ellipsis-v bigger-120 font12" data-toggle="tooltip" title="Menu" style="margin-top: 15px;"></li></span>' +
                '</a > ') : '') +

        '</div>';
    return actionsHtml;
}

$(document).ready(function () {
    $('[data-toggle="tooltip"]').tooltip();
});

function GetMemberNpmineeUpdateAction(rowId, dtCommonParam,) {
    var actionsHtml = '<div>' +
        (dtCommonParam.updateUrl ?
            ('<a  onclick="PopulateMemberNominee(' + rowId + ')" class="btn-success btn-sm">' +
                '<span class="waves-light"><li class="ace-icon  fa fa-pencil-square-o bigger-120 font12" title="EditNominee" style="margin-right: 5px;"></li></span>' +
                '</a > ') : '') +

        '</div>';
    return actionsHtml;
}
//for Add and Edit
function GetEditAddActions(rowId, dtCommonParam, username) {
    var actionsHtml = '<div>' +
        (dtCommonParam.addUrl ?
            ('<a href=" ' + dtCommonParam.updateUrl + rowId + ' " class="tooltip-success">' +
                '<span class="waves-light"><li class="fa fa-pencil font12" title="Edit" style="margin-right: 15px;"></li></span>' +
                '</a > ') : '') +
        (dtCommonParam.addUrl ?
            ('<a href=" ' + dtCommonParam.addUrl + rowId + ' " class="tooltip-success">' +
                '<span class="waves-light"><li class="ace-icon fa fa-plus bigger-120" title="Add " style="margin-right: 5px;"></li></span>' +
                '</a > ') : '') +

        '</div>';
    return actionsHtml;
}
//Get CheckBox
function GetChecKBox(rowId, membershipnmbr) {

    var actionsHtml = '<div>' +
        '<input type="checkbox" value=" ' + rowId + ' "  id="' + rowId + "" + rowId + '" onclick="VerifyMemberforAllotment(' + "'" + rowId + "" + rowId + "'," + "'" + rowId + "'" + ')" name="SelectedMemberCheckBox" class="SelectedMemberCheckBox" >' +
        '</div>';
    return actionsHtml;

}
//Edit View Button
function GetEditViewActionsForComponent(rowId, dtCommonParam, rowid) {
    var actionsHtml = '<div>' +
        (dtCommonParam.addUrl ?
            ('<a href=" ' + dtCommonParam.updateUrl + rowId + ' " class="btn-success btn-sm">' +
                '<span class="waves-light"><li class="fa fa-pencil font12" title="Edit" style="margin-right: 5px;"></li></span>' +
                '</a > ') : '') +
        (dtCommonParam.DetailViewUrl ?
            ('<a href=" ' + dtCommonParam.DetailViewUrl + rowid + ' " class="btn-success btn-sm">' +
                '<span class="waves-light"><li class="ace-icon fa fa-plus bigger-120" title="Add " style="margin-right: 5px;"></li></span>' +
                '</a > ') : '') +
        (dtCommonParam.deleteUrl ?
        ('<a  onclick="ShowDeleteDialog(' + rowid + ')"   class="btn-success btn-sm">' +
                '<span class="waves-light"><li class="ace-icon fa fa-trash bigger-120" title="Delete " style="margin-right: 5px;"></li></span>' +
                '</a > ') : '') +

        '</div>';
    return actionsHtml;
}

//EditCompponentActions
function GetEditLandComponentViewActions(rowId, dtCommonParam) {
    var actionsHtml = '<div>' +
        (dtCommonParam.DetailViewUrl ?
            ('<a href=" ' + dtCommonParam.DetailViewUrl + rowId + '&ReturnId=' + dtCommonParam.mainid + ' " class="tooltip-success">' +
                '<span class="waves-light"><li class="fa fa-eye font12" title="Component Detail" style="margin-right: 15px;"></li></span>' +
                '</a > ') : '') +
        (dtCommonParam.updateUrl ?
            ('<a href=" ' + dtCommonParam.updateUrl + rowId + '&ReturnId=' + dtCommonParam.mainid + ' " class="tooltip-success">' +
                '<span class="waves-light"><li class="fa fa-pencil font12" title="Edit" style="margin-right: 15px;"></li></span>' +
                '</a > ') : '') +
        (dtCommonParam.deleteUrl ?
        ('<a   onclick="ShowDeleteComponentDialog(' + rowId + "," + dtCommonParam.mainid + ')"  class="tooltip-success">' +
                '<span class="waves-light"><li class="fa fa-trash font12" title="Delete" style="margin-right: 15px;"></li></span>' +
                '</a > ') : '') +

        '</div>';
    return actionsHtml;
}
//MemberAllotmentActions
function GetMemberPlotAllotmentActions(rowId, dtCommonParam) {
    var actionsHtml = '<div>' +
        (dtCommonParam.DetailsUrl ?
            ('<a href=" ' + dtCommonParam.DetailsUrl + rowId + ' " class="btn-success btn-sm">' +
                '<span class="waves-light"><li class="fa fa-eye font12" title="Details" data-toggle="tooltip" style="margin-right: 5px;"></li></span>' +
                '</a > ') : '') +
        (dtCommonParam.HistoryUrl ?
            ('<a href=" ' + dtCommonParam.HistoryUrl + rowId + ' " class="btn-success btn-sm">' +
                '<span class="waves-light"><li class="fa fa-history font12" title="History" data-toggle="tooltip" style="margin-right: 5px;"></li></span>' +
                '</a > ') : '') +

        '</div>';
    return actionsHtml;
}
//AddEditComponent
//
function GetEditViewActions(rowId, dtCommonParam) {
    var actionsHtml = '<div>' +
        (dtCommonParam.updateUrl ?
            ('<a href=" ' + dtCommonParam.updateUrl + rowId + ' " class="btn-success btn-sm">' +
                '<span class="waves-light"><li class="fa fa-pencil font12" title="Edit" style="margin-right: 1px;"></li></span>' +
                '</a > ') : '') +
        (dtCommonParam.DetailViewUrl ?
            ('<a href=" ' + dtCommonParam.DetailViewUrl + rowId + ' " class="btn-success btn-sm">' +
                '<span class="waves-light"><li class="ace-icon fa fa-eye bigger-120" title="View" style="margin-right: 1px;"></li></span>' +
                '</a > ') : '') +

        '</div>';
    return actionsHtml;
}
///Get Action
function GetAction(rowId, dtCommonParam, username) {
    var actionsHtml = '<div>' +
        (dtCommonParam.resetPasswordUrl ?
            ('<a target="_blank" href=" ' + dtCommonParam.resetPasswordUrl + username + ' " class="text-primary mr-2">' +
                '<span class="waves-light"><li class="ace-icon fa fa-undo bigger-120" title="Reset Password" style="margin-right: 5px;"></li></span>' +
                '</a >') : '') +
        (dtCommonParam.updateUrl ?
            ('<a href=" ' + dtCommonParam.updateUrl + rowId + ' " class="tooltip-success">' +
                '<span class="waves-light"><li class="ace-icon fa fa-pencil-square-o bigger-120" title="Edit" style="margin-right: 5px;"></li></span>' +
                '</a > ') : '') +

        '</div>';
    return actionsHtml;
}
//For Employee
function GetgridStatusDescription(Status, rowId, dtCommonParam) {
    var flagStatus, styleClasses, actionHtml;
    if (Status == "Pending") {
        flagStatus = "Pending";
        styleClasses = "fa fa-dot-circle-o text-info";

        actionHtml = '<div class="dropdown action-label">' +
            '<a class="btn btn-white btn-sm btn-rounded dropdown-toggle" href="#" data-toggle="dropdown" aria-expanded="false">' +
            '<i class=' + '"' + styleClasses + '"' + '></i>' + flagStatus +
            '</a>' +
            '<div class="dropdown-menu dropdown-menu-right">' +
            '<a class="dropdown-item" href="#" onclick ="ShowCancelModel(' + "'" + dtCommonParam.cancelUrl + rowId + "'," + "'" + dtCommonParam.tableId + "'" + ')"><i class="fa fa-dot-circle-o text-danger"></i> Cancel</a>' +
            '</div>' +
            '</div>';
        return actionHtml;
    }
    else if (Status == "New") {
        flagStatus = " New";
        styleClasses = "fa fa-dot-circle-o text-purple";

        actionHtml = '<div class="dropdown action-label">' +
            '<a class="btn btn-white btn-sm btn-rounded dropdown-toggle" href="#" data-toggle="dropdown" aria-expanded="false">' +
            '<i class=' + '"' + styleClasses + '"' + '></i>' + flagStatus +
            '</a>' +
            '<div class="dropdown-menu dropdown-menu-right">' +
            '<a class="dropdown-item" href="#" onclick ="ShowCancelModel(' + "'" + dtCommonParam.cancelUrl + rowId + "'," + "'" + dtCommonParam.tableId + "'" + ')"><i class="fa fa-dot-circle-o text-danger"></i> Cancel</a>' +
            '</div>' +
            '</div>';
        return actionHtml;
    }
    else if (Status == "Submited") {
        flagStatus = " Submited";
        styleClasses = "fa fa-dot-circle-o text-purple";

        actionHtml = '<div class="dropdown action-label">' +
            '<a class="btn btn-white btn-sm btn-rounded dropdown-toggle" href="#" data-toggle="dropdown" aria-expanded="false">' +
            '<i class=' + '"' + styleClasses + '"' + '></i>' + flagStatus +
            '</a>' +
            '<div class="dropdown-menu dropdown-menu-right">' +
            '<a class="dropdown-item" href="#" onclick ="ShowCancelModel(' + "'" + dtCommonParam.cancelUrl + rowId + "'," + "'" + dtCommonParam.tableId + "'" + ')"><i class="fa fa-dot-circle-o text-danger"></i> Cancel</a>' +
            '</div>' +
            '</div>';
        return actionHtml;
    }
    else if (Status == "Inprocess") {
        flagStatus = " In Process";
        styleClasses = "fa fa-dot-circle-o text-info";

        actionHtml = '<div class="dropdown action-label">' +
            '<a class="btn btn-white btn-sm btn-rounded dropdown-toggle" href="#" data-toggle="dropdown" aria-expanded="false">' +
            '<i class=' + '"' + styleClasses + '"' + '></i>' + flagStatus +
            '</a>' +
            '<div class="dropdown-menu dropdown-menu-right">' +
            '<a class="dropdown-item" href="#" onclick ="ShowCloseModel(' + "'" + dtCommonParam.closeUrl + rowId + "'," + "'" + dtCommonParam.tableId + "'" + ')"><i class="fa fa-dot-circle-o text-success"></i> Close</a>' +
            '<a class="dropdown-item" href="#" onclick ="ShowCancelModel(' + "'" + dtCommonParam.cancelUrl + rowId + "'," + "'" + dtCommonParam.tableId + "'" + ')"><i class="fa fa-dot-circle-o text-danger"></i> Cancel</a>' +
            '</div>' +
            '</div>';
        return actionHtml;
    }
    else if (Status == "Closed") {
        flagStatus = " Closed"
        styleClasses = "fa fa-dot-circle-o text-success";

        actionHtml = '<div class="dropdown action-label">' +
            '<a class="btn btn-white btn-sm btn-rounded dropdown-toggle" href="#" data-toggle="dropdown" aria-expanded="false">' +
            '<i class=' + '"' + styleClasses + '"' + '></i>' + flagStatus +
            '</a>' +
            '</div>';
        return actionHtml;
    }
    else if (Status == "Approved") {
        flagStatus = " Approved"
        styleClasses = "fa fa-dot-circle-o text-success";

        actionHtml = '<div class="dropdown action-label">' +
            '<a class="btn btn-white btn-sm btn-rounded" href="#" data-toggle="dropdown" aria-expanded="false">' +
            '<i class=' + '"' + styleClasses + '"' + '></i>' + flagStatus +
            '</a>' +
            '</div>';
        return actionHtml;
    }
    else if (Status == "Rejected") {
        flagStatus = " Rejected";
        styleClasses = "fa fa-dot-circle-o text-danger";

        actionHtml = '<div class="dropdown action-label">' +
            '<a class="btn btn-white btn-sm btn-rounded" href="#" data-toggle="dropdown" aria-expanded="false">' +
            '<i class=' + '"' + styleClasses + '"' + '></i>' + flagStatus +
            '</a>' +
            '</div>';
        return actionHtml
    }
    else {
        flagStatus = " Cancelled";
        styleClasses = "fa " + "fa-dot-circle-o " + "text-danger";

        actionHtml = '<div class="dropdown action-label">' +
            '<a class="btn btn-white btn-sm btn-rounded dropdown-toggle" href="#" data-toggle="dropdown" aria-expanded="false">' +
            '<i class=' + '"' + styleClasses + '"' + '></i>' + flagStatus +
            '</a>' +
            '</div>';
        return actionHtml;
    }
}

//For Manager
function GetManagerAction(rowId, dtCommonParam) {

    var actionHtml = '<div class="dropdown dropdown-action">' +
        '<a href="#" class="action-icon dropdown-toggle" data-toggle="dropdown" aria-expanded="false"><i class="material-icons">more_vert</i></a>' +
        '<div class="dropdown-menu dropdown-menu-right">' +
        '<a class="dropdown-item" href="#" onclick ="ShowApprovedModal(' + "'" + dtCommonParam.approvedUrl + rowId + "'," + "'" + dtCommonParam.tableId + "'" + ')"' + '><i class="fa fa-dot-circle-o text-success"></i> Approve</a>' +
        '<a class="dropdown-item" href="#" onclick ="ShowRejectedModel(' + "'" + dtCommonParam.rejectedUrl + rowId + "'," + "'" + dtCommonParam.tableId + "'" + ')"><i class="fa fa-dot-circle-o text-danger"></i> Reject</a>' +
        '</div>' +
        '</div>';
    return actionHtml;

}

function GetgridButton(rowId, ValidFlag, dtCommonParam, refNumber) {
    if (ValidFlag) {
        var actionsHtml = '<div>' +

            (dtCommonParam.updateUrl ?
                ('<a href=" ' + dtCommonParam.updateUrl + rowId + ' " class="tooltip-success">' +
                    '<span class="waves-light"><li class="ace-icon fa fa-pencil-square-o bigger-120" title="Edit" style="margin-right: 5px;"></li></span>' +
                    '</a > ') : '')
            +

            (dtCommonParam.approveUrl ?
                ('<button type="button" class="btn btn-primary waves-effect waves-light" style="float: none;margin: 5px;" onclick="RedirectToUrl(' + "'" + dtCommonParam.approveUrl + refNumber + "'" + ')">' +
                    '   ' +
                    '<span class="icofont icofont-tick-mark"></span>' +
                    '   '
                    +
                    '</button>') :
                '')
            +

            (dtCommonParam.approveOnScreen ?
                ('<button type="button" class="btn btn-primary waves-effect waves-light" style="float: none;margin: 5px;" data-toggle="modal" data-target="#OnScreenApprovalModal"' + ')">' +
                    '   ' +
                    '<span class="icofont icofont-tick-mark"></span>' +
                    '   '
                    +
                    '</button>') :
                '')
        //+

        //(dtCommonParam.deActivateUrl ?
        //    (
        //    '<a href="#"><li  class="fa fa-dot-circle-o text-danger" title="Inactive" onclick ="ShowModal(' + "'" + dtCommonParam.deActivateUrl + rowId + "'," + "'" + dtCommonParam.tableId + "'," + "'" + "inactive'" +')">' +
        //        '</li></a>') : '') +
        '</div>';
        return actionsHtml;
    }
    else {
        var actionsHtml = '<div>' +

            (dtCommonParam.updateUrl ?
                ('<a href=" ' + dtCommonParam.updateUrl + rowId + ' " class="tooltip-success">' +
                    '<span class="waves-light"><li class="ace-icon fa fa-pencil-square-o bigger-120" title="Edit" style="margin-right: 5px;"></li></span>' +
                    '</a > ') : '')
            +
            (dtCommonParam.approveUrl ?
                ('<button type="button" class="btn btn-primary waves-effect waves-light" style="float: none;margin: 5px;" onclick="RedirectToUrl(' + "'" + dtCommonParam.approveUrl + refNumber + "'" + ')">' +
                    '   ' +
                    '<span class="icofont icofont-tick-mark"></span>' +
                    '   '
                    +
                    '</button>') :
                '')
            +
            (dtCommonParam.approveOnScreen ?
                ('<button type="button" class="btn btn-primary waves-effect waves-light" style="float: none;margin: 5px;" data-toggle="modal" data-target="#OnScreenApprovalModal" onclick="' + dtCommonParam.approveOnScreenFunction + '(' + "'" + refNumber + "'" + ')">' +
                    '   ' +
                    '<span class="icofont icofont-tick-mark"></span>' +
                    '   '
                    +
                    '</button>') :
                '')
            +
            //(dtCommonParam.activateUrl ? (
            //'<a href="#"><li  class="fa fa-dot-circle-o text-success" title="Active" onclick ="ShowModal(' + "'" + dtCommonParam.activateUrl + rowId + "'," + "'" + dtCommonParam.tableId + "'," + "'" + "active'"+ ')">' +
            //'</li></a>') : '') +
            '</div>';
        return actionsHtml;
    }
};

function GetgridPriceButton(rowId, ValidFlag, dtCommonParam, refNumber) {
    if (ValidFlag) {
        var actionsHtml = '<div class="btn-group btn-group-sm" style="float: none;">' +
            '   ' +
            (dtCommonParam.updateUrl ?
                ('<button type="button" class="btn btn-primary waves-effect waves-light" style="float: none;margin: 5px;" onclick="UpdatePrice(' + "'" + rowId + "'" + ')">' +
                    '   ' +
                    '<span class="icofont icofont-ui-edit"></span>' +
                    '   '
                    +
                    '</button>') :
                '')
        return actionsHtml;
    }
};

function GetgridViewButton(rowId, ValidFlag, dtCommonParam) {
    if (ValidFlag) {
        var actionsHtml = '<div class="btn-group btn-group-sm" style="float: none;">' +
            '   ' +
            (dtCommonParam.updateUrl ?
                ('<button type="button" class="btn btn-primary waves-effect waves-light" style="float: none;margin: 5px;" onclick="RedirectToUrl(' + "'" + dtCommonParam.updateUrl + rowId + "'" + ')">' +
                    '   ' +
                    '<span class="icofont icofont-ui-edit"></span>' +
                    '   '
                    +
                    '</button>') :
                '')
        return actionsHtml;
    }
};

function GetPrintButton(refNumber, ValidFlag, dtCommonParam, printFunc) {
    var actionsHtml = '<div class="btn-group btn-group-sm" style="float: none;">' +
        '   ' +
        '<button type="button" class="btn btn-primary waves-effect waves-light" style="float: none;margin: 5px;" onclick=" ' + printFunc + ' (' + "'" + refNumber + "'" + ')">' +
        '   ' +
        '<span class="fa fa-print"></span>' +
        '   '
        +
        '</button>' +
        '</div>';
    return actionsHtml;
}

function GetButtonForAdjustment(refNumber, ValidFlag, dtCommonParam, printFunc) {
    var actionsHtml = '<div class="btn-group btn-group-sm" style="float: none;">' +
        '   ' +
        '<button type="button" class="btn btn-primary waves-effect waves-light" style="float: none;margin: 5px;" onclick=" ' + printFunc + ' (' + "'" + refNumber + "'" + ')">' +
        '   ' +
        '<span class="fa fa-print"></span>' +
        '   '
        +
        '</button>' +
        '</div>';
    return actionsHtml;
}


function ShowModal(url, id, status) {

    dataUrl = url;
    tableId = id;
    if (status == "active") {
        $("#active").modal("toggle");
    }
    if (status == "inactive") {
        $("#inactive").modal("toggle");
    }
}

function ShowCancelModel(url, id) {

    dataUrl = url;
    tableId = id;
    $("#cancelModel").modal("toggle");
}

function ShowApprovedModal(url, id) {
    dataUrl = url;
    tableId = id;
    $("#Approved").modal("toggle");
}

function ShowRejectedModel(url, id) {
    dataUrl = url;
    tableId = id;
    $("#Rejected").modal("toggle");
}

function ShowCloseModel(url, id) {
    dataUrl = url;
    tableId = id;
    $("#Closed").modal("toggle");
}



function GetHoursAndMinutesByDecimalHours(DecimalHours) {

    var decimalTime = parseFloat(DecimalHours);
    decimalTime = decimalTime * 60 * 60;
    var hours = Math.floor((decimalTime / (60 * 60)));
    decimalTime = decimalTime - (hours * 60 * 60);
    var minutes = Math.floor((decimalTime / 60));
    decimalTime = decimalTime - (minutes * 60);
    var seconds = Math.round(decimalTime);
    if (hours < 10) {
        hours = "0" + hours;
    }
    if (minutes < 10) {
        minutes = "0" + minutes;
    }
    if (seconds < 10) {
        seconds = "0" + seconds;
    }
    var HoursAndMinute = "" + hours + ":" + minutes;
    return HoursAndMinute;
    // alert("" + hours + ":" + minutes + ":" + seconds);
}
function GetDivisionbyProvince() {
    var provinceid = $("#Province").val()
    LoadKendoDropDown('Division', '-- Please Select Division --', '../api/CommonValues/GetDivisionByProvince?id=' + provinceid);
}
function GetDistrictbyDivision() {


    var divisionid = $("#Division").val()
    if (divisionid == "") {
        // UnselectKendoDropDown("District");
        LoadDistrictKendoDropDown('District', '-- Please Select District --', '');
        // UnselectKendoDropDown("Tehsil");
        LoadTehsilKendoDropDown('Tehsil', '-- Please Select Tehsil --', '');
    }
    else {
        LoadKendoDropDown('District', '-- Please Select District --', '../api/CommonValues/GetDistrictbyDivision?id=' + divisionid);
    }
}
function GetTehsilsbyDistrict() {

    var districtid = $("#District").val()

    if (districtid == "") {
        //UnselectKendoDropDown("Tehsil");
        LoadTehsilKendoDropDown('Tehsil', '-- Please Select Tehsil --', '');
    }
    else {
        LoadKendoDropDown('Tehsil', '-- Please Select Tehsils --', '../api/CommonValues/GetTehsilsbyDistrict?id=' + districtid);
    }
}
function GetSubDomain(id) {

    if (id == "") {
        UnselectKendoDropDown("SubDomain");
        LoadTehsilKendoDropDown('SubDomain', '-- Please Select Sub-Domain --', '');
    }
    else {
        LoadKendoDropDown('SubDomain', '-- Please Select Sub-Domain --', '../api/CommonValues/GetSubDomainbyDomain?id=' + id);
    }
}
function GetSocitiesByTehsils(id) {

    if (id == "") {
        LoadKendoDropDown('societies', '-- Please Select Society --', '');
    }
    else {
        LoadKendoDropDown('societies', '-- Please Select Society --', '../api/CommonValues/GetSocitiesbyTehsil?id=' + id);

    }
}
function GetOfficerByPost(id) {

    if (id == "") {

        UnselectKendoDropDown("Officer");

        LoadTehsilKendoDropDown('Officer', '-- Please Select Officers --', '');
    }
    else {

        LoadKendoDropDown('Officer', '-- Please Select Officers --', '../api/CommonValues/GetOfficerByPost?id=' + id);
    }
}

function loadPhaseExtensionBySociety(id) {

    console.log("M Anis");
    if (id == "") {

        UnselectKendoDropDown("phaseext");

        LoadKendoDropDown('phaseext', '-- Please Select Phase/Extension --', '');
        LoadKendoDropDown('societysector', '-- Please Select Sector --', '');
        LoadKendoDropDown('societyblock', '-- Please Select Block --', '');
    }
    else {

        LoadKendoDropDown('phaseext', '-- Please Select Phase/Extension --', '../api/PhaseExtension/GetActivePhaseExtensionsBySociety?id=' + id);
        LoadKendoDropDown('societyblock', '-- Please Select Block --', '../api/CommonValues/GetAllSocietyBlocksBySocietyId?id=' + id);
    }

}
function LoadSocitiesServerKendoDropDown(id, placeholder, data) {

    $("#" + id).kendoComboBox({
        dataTextField: "SocietyName",
        dataValueField: "SocietyOID",
        optionLabel: placeholder,
        filter: "contains",
        autoBind: false,
        minLength: 3,
        height: 200,

        dataSource: {
            type: "odata",
            serverFiltering: true,
            transport: {
                read: {
                    url: data,
                }
            }
        }
    });
};

$('.aplhabetsAllowed').keydown(function (e) {
    if (e.shiftKey || e.ctrlKey || e.altKey) {
        e.preventDefault();
    } else {

        var key = e.keyCode;
        if (!((key == 8) || (key == 9) || (key == 16) || (key == 32) || (key == 46) || (key >= 35 && key <= 40) || (key >= 65 && key <= 90))) {
            e.preventDefault();
            //toastr.warning("only alphabets were allowed");
        }
    }
});


//$(".validatePH").keydown(function (event) {
//   

//    // Allow only backspace,delete,left arrow,right arraow and Tab
//    if (event.keyCode == 46
//        || event.keyCode == 8
//        || event.keyCode == 37
//        || event.keyCode == 39
//        || event.keyCode == 9) {
//        // let it happen, don't do anything
//    }
//    else {
//        // Ensure that it is a number and stop the keypress
//        if ((event.keyCode < 48 || event.keyCode > 57) && (event.keyCode < 96 || event.keyCode > 105)) {
//            event.preventDefault();
//        }
//    }
//});

//$('.validatePH').keydown( function () {
//  
//    if ($('.validatePH').val().length > 21) {
//        $('.validatePH').val($('.validatePH').val().substr(0, 20));
//    }
//});



$('.phvalidate').on('keydown keyup change', function () {

    var char = $(this).val();
    var charLength = $(this).val().length;
    if (charLength > 20) {
        $(this).val(char.substring(0, 20));
    } else {
        $('#warning-message').text('');
    }
});

//$('#share').on('keydown keyup change', function () {
//    
//    var char = $(this).val();
//    var charLength = $(this).val().length;
//    if (charLength > 3) {
//        $(this).val(char.substring(0, 3));
//    } else {
//        $('#warning-message').text('');
//    }
//});

//$('#mobile').on('keydown keyup change', function () {

//    var char = $(this).val();
//    var charLength = $(this).val().length;
//    if (charLength > 15) {
//        $(this).val(char.substring(0, 15));
//    } else {
//        $('#warning-message').text('');
//    }
//});

$(".number_only").keypress(function (e) {

    if (String.fromCharCode(e.keyCode).match(/^\d+(\.\d{1,2})?$/g)) return false;
});

function dataURLtoFile(dataurl, filename) {

    var arr = dataurl.split(','),
        mime = arr[0].match(/:(.*?);/)[1],
        bstr = atob(arr[1]),
        n = bstr.length,
        u8arr = new Uint8Array(n);

    while (n--) {
        u8arr[n] = bstr.charCodeAt(n);
    }

    return new File([u8arr], filename, { type: mime });
}

function ShowChangePasswordModal() {
    $("#passwordmodal").modal('toggle');
}
function ChangePassword() {

    var data = {

        OldPassword: $("#oldpass").val(),
        NewPassword: $("#newpass").val(),
        ConfirmNewPassword: $("#newconpass").val(),


    };

    if (data.OldPassword == "") { toastr.warning("Old Password Is Required", { timeOut: 5000 }); }
    else if (data.NewPassword == "") { toastr.warning("New Password Is Required", { timeOut: 5000 }); }
    else if (CheckChangedpassword(data.NewPassword) == false) { toastr.warning("password must be a minimum of 6 characters including One Uppercase one Lowercase one number and one special character i.e (@)", { timeOut: 5000 }); }
    else if (data.NewPassword != data.ConfirmNewPassword) { toastr.warning("New and confirm password must be same.", { timeOut: 5000 }); }
    else {

        $.ajax({

            url: '../api/User/ChangePassword',
            type: 'POST',
            data: JSON.stringify(data),
            contentType: "application/json;charset=utf-8",
            success: function (data) {
                toastr.success("Password Changed Successfully!", { timeOut: 5000 });
                RedirectToUrl("../Accounts/LogOff");


            },

            error: function (response) {
                var response = response.responseText.replace(/"/g, '');
                toastr.error(response, { timeOut: 5000 });

            }
        });
    }

}
function ChangeUserPassword() {

    var data = {

        User_Id: $("#User_Id").val(),
        NewPassword: $("#passtochange").val(),


    };

     if (data.NewPassword == "") { toastr.warning("New Password Is Required", { timeOut: 5000 }); }
    else if (CheckChangedpassword(data.NewPassword) == false) { toastr.warning("password must be a minimum of 6 characters including One Uppercase one Lowercase one number and one special character i.e (@)", { timeOut: 5000 }); }

    else {

        $.ajax({

            url: '../api/User/ChangeUserPassword',
            type: 'POST',
            data: JSON.stringify(data),
            contentType: "application/json;charset=utf-8",
            success: function (data) {
                
                toastr.success("Password Changed Successfully!", { timeOut: 5000 });
                $("#changepasswordmodal").modal('toggle');

            },

            error: function (response) {
                var response = response.responseText.replace(/"/g, '');
                toastr.error(response, { timeOut: 5000 });

            }
        });
    }

}
function CheckChangedpassword(Password) {
    
    var regix = new RegExp("^(?=.*[a-z])(?=.*[A-Z])(?=.*[0-9])(?=.*[!@#\$%\^&\*])(?=.{6,})");

    if (regix.test(Password) == false) {
        return false;
    }
    else { return true; }



}
function DownloadFiles(value) {
    
    if (value == "") {
        toastr.error("File Not Exist", { timeOut: 5000 });
    }
    else {

        Downloadfilepath = value;
        
        ShowLoader();
        $.ajax({
            url: "../api/CommonValues/DownloadFile?filename=" + Downloadfilepath,
            type: 'GET',
            contentType: "application/json;charset=utf-8",
            success: function () {
                window.location = "../api/CommonValues/DownloadFile?filename=" + Downloadfilepath;
                
                HideLoader();
            },
            error: function (data) {
                var response = data.responseText.replace(/"/g, '');
                toastr.error(response, { timeOut: 5000 });

                HideLoader();
            }
        });
    }
}
function isValidPhoneNumber(number) {
    // Regular expression for Pakistani phone number validation
    var phoneRegex = /^(?:\+92|92)?(?:\d{10}|\d{3}-\d{7})$/;
    return phoneRegex.test(number);
}
function isValidCNIC(cnic) {
    // Regular expression for CNIC validation
    var cnicRegex = /^[0-9+]{5}-[0-9+]{7}-[0-9]{1}$/;
    //if (cnicRegex.test(cnic)) {
    //    //console.log("CNCI True");
    //    return true;
    //} else {
    //    //console.log("CNCI false");
    //    return false;
    //}
    return cnicRegex.test(cnic);
}
function isValidEmail(email) {
    // Regular expression for basic email validation
    var emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    return emailRegex.test(email);
}



function LoadExpenseTypesKendoDropDown(id, placeholder, url) {

    var promiseObject = GetPromise(url);
    promiseObject
        .then(data =>
            SetExpenseTypesKendoDDSource(id, placeholder, data))
        .catch(error =>
            console.log("KENDO DROPDOWN ERROR" + "/r/n" + "Url:" + url + "/r/n" + "htmlElementId:" + id + "/r/n" + "Error Detail" + error));
};

function SetExpenseTypesKendoDDSource(id, placeholder, data) {

    try {
        var dropdownlist = $("#" + id).data("kendoDropDownList");
        dropdownlist.destroy();

    } catch (e) {

    }

    $("#" + id).kendoDropDownList({

        dataTextField: "text",
        dataValueField: "value",
        optionLabel: placeholder,
        dataSource: data,
        sort: { field: "text", dir: "asc" },
        filter: "contains",
        height: 200,
        dataBound: function () {
            if (selectedValue) {
                this.value(selectedValue); // SET SELECTED VALUE BACK
            }
        },
        popup: {
            appendTo: $("#" + id + "modal")
        }
    });


};


function allowOnlyUrdu(input) {
    // Urdu Unicode range
    const urduRegex = /[^\u0600-\u06FF\s]/g;
    input.value = input.value.replace(urduRegex, '');
}


//function LoadKendoMultiselectWithCheckBox(id, placeholder, url, callback = null) {
//    var promiseObject = GetPromise(url);
//    promiseObject
//        .then(data =>
//            SetKendoMultiSourceCheckBox(id, placeholder, data))
//        .catch(error =>
//            console.log("KENDO MULTISELECT ERROR" + "/r/n" + "Url:" + url + "/r/n" + "htmlElementId:" + id + "/r/n" + "Error Detail" + error));
//    if (callback != null)
//        callback();
//};
//function SetKendoMultiSourceCheckBox(id, placeholder, data2) {
//        var ds = new kendo.data.DataSource({
//             data: data2
//         });
//    $("#" + id).kendoMultiSelect({    
//        dataValueField: "Id",
//        dataTextField: "Name",
//        optionLabel: placeholder,
//        placeholder: placeholder,
//        dataSource: ds,
//        //height: 100,
//        dataBound: function () {
//            var items = this.ul.find("li");
//            setTimeout(function () {
//                checkInputs(items);
//            });
//        },
//        itemTemplate: "<input type='checkbox'/> #:data.Name#",
//        headerTemplate: "<div><input type='checkbox' id='Header'><label> Select All</label></div>",
//        autoClose: false,
//        change: function () {
//            var items = this.ul.find("li");
//            checkInputs(items);
//            updateHeaderCheckbox(); // Update header checkbox based on selection
//        }
//    });  
//};

/**
 * IMS Common Multi-Select Combo Box - Reusable for Create/Edit and other forms.
 * Supports single-select (dropdown) and multi-select modes; searchable; syncs with hidden input(s) for form submit.
 * Usage: IMSCommonCombo.init('containerId', { data: [...], valueField: 'id', textField: 'name', inputName: 'BranchId', selectedValue: 1 });
 */
var IMSCommonCombo = (function () {
    'use strict';

    function escapeHtml(text) {
        if (text == null) return '';
        var div = document.createElement('div');
        div.textContent = text;
        return div.innerHTML;
    }

    function addStyles() {
        if (document.getElementById('ims-common-combo-styles')) return;
        var css = [
            '.ims-combo-wrapper { position: relative; width: 100%; }',
            '.ims-combo-display { display: flex; align-items: center; justify-content: space-between; min-height: 38px; padding: 6px 12px; border: 1px solid #ced4da; border-radius: 6px; background: #fff; cursor: pointer; user-select: none; }',
            '.ims-combo-display:hover { border-color: #86b7fe; }',
            '.ims-combo-display.open { border-color: #86b7fe; box-shadow: 0 0 0 0.25rem rgba(13, 110, 253, 0.25); }',
            '.ims-combo-display .ims-combo-text { flex: 1; color: #212529; min-width: 0; overflow: hidden; }',
            '.ims-combo-display .ims-combo-placeholder { color: #6c757d; }',
            '.ims-combo-display .ims-combo-arrow { margin-left: 8px; color: #6c757d; font-size: 0.7em; transition: transform 0.2s; }',
            '.ims-combo-display.open .ims-combo-arrow { transform: rotate(180deg); }',
            '.ims-combo-panel { display: none; position: absolute; top: 100%; left: 0; right: 0; z-index: 1050; margin-top: 2px; background: #fff; border: 1px solid #ced4da; border-radius: 6px; box-shadow: 0 4px 12px rgba(0,0,0,0.15); max-height: 280px; min-height: 80px; }',
            '.ims-combo-panel.open { display: block !important; visibility: visible; }',
            '.ims-combo-search { width: 100%; padding: 8px 12px; border: none; border-bottom: 1px solid #dee2e6; border-radius: 6px 6px 0 0; box-sizing: border-box; }',
            '.ims-combo-search:focus { outline: none; }',
            '.ims-combo-list { max-height: 220px; overflow-y: auto; padding: 4px 0; }',
            '.ims-combo-item { padding: 8px 12px; cursor: pointer; display: flex; align-items: center; }',
            '.ims-combo-item:hover { background: #f8f9fa; }',
            '.ims-combo-item.selected { background: #e7f1ff; color: #0d6efd; }',
            '.ims-combo-item input[type="checkbox"] { margin-right: 8px; }',
            '.ims-combo-no-results { padding: 12px; color: #6c757d; text-align: center; }',
            '.ims-combo-tags { display: flex; flex-wrap: wrap; gap: 4px; margin-top: 4px; }',
            '.ims-combo-tag { display: inline-flex; align-items: center; padding: 2px 8px; background: #e9ecef; border-radius: 4px; font-size: 0.875rem; }',
            '.ims-combo-tag .ims-combo-tag-remove { margin-left: 6px; cursor: pointer; color: #6c757d; }',
            '.ims-combo-tag .ims-combo-tag-remove:hover { color: #dc3545; }'
        ].join('\n');
        var style = document.createElement('style');
        style.id = 'ims-common-combo-styles';
        style.textContent = css;
        document.head.appendChild(style);
    }

    function init(containerId, options) {
        var container = document.getElementById(containerId);
        if (!container) {
            console.warn('IMSCommonCombo: container not found:', containerId);
            return null;
        }

        addStyles();

        var opts = options || {};
        var data = Array.isArray(opts.data) ? opts.data : [];
        var valueField = opts.valueField || 'value';
        var textField = opts.textField || 'text';
        var placeholder = opts.placeholder || '-- Select --';
        var multiSelect = !!opts.multiSelect;
        var inputName = opts.inputName || 'comboValue';
        var selectedValue = opts.selectedValue;
        var selectedValues = opts.selectedValues || (selectedValue != null ? [selectedValue] : []);

        function toStr(v) {
            return v != null && v !== '' ? String(v) : '';
        }
        var initialSelected = multiSelect ? selectedValues : (selectedValue != null ? [selectedValue] : []);
        var normalizedSelected = initialSelected.map(function (v) { return toStr(v); }).filter(function (s) { return s !== ''; });

        var state = {
            open: false,
            selected: normalizedSelected,
            filtered: data.slice()
        };

        var hiddenInput = container.querySelector('input[type="hidden"]');
        if (!hiddenInput) {
            hiddenInput = document.createElement('input');
            hiddenInput.type = 'hidden';
            hiddenInput.name = inputName;
        }
        hiddenInput.name = inputName;

        function getItemText(item) {
            var t = item[textField];
            return t != null ? String(t) : '';
        }

        function getItemValue(item) {
            var v = item[valueField];
            return toStr(v);
        }

        function getSelectedItems() {
            return data.filter(function (item) {
                return state.selected.indexOf(getItemValue(item)) !== -1;
            });
        }

        function updateHiddenInput() {
            if (multiSelect) {
                hiddenInput.value = state.selected.join(',');
            } else {
                hiddenInput.value = state.selected.length ? state.selected[0] : '';
            }
        }

        function renderDisplay() {
            var textEl = container.querySelector('.ims-combo-text');
            if (!textEl) return;
            var items = getSelectedItems();
            if (items.length === 0) {
                textEl.textContent = placeholder;
                textEl.classList.add('ims-combo-placeholder');
            } else {
                textEl.classList.remove('ims-combo-placeholder');
                if (multiSelect) {
                    textEl.innerHTML = '';
                    var tagsDiv = document.createElement('div');
                    tagsDiv.className = 'ims-combo-tags';
                    items.forEach(function (item) {
                        var tag = document.createElement('span');
                        tag.className = 'ims-combo-tag';
                        tag.innerHTML = escapeHtml(getItemText(item)) + ' <span class="ims-combo-tag-remove" data-value="' + escapeHtml(getItemValue(item)) + '">&times;</span>';
                        tagsDiv.appendChild(tag);
                    });
                    textEl.appendChild(tagsDiv);
                } else {
                    textEl.textContent = getItemText(items[0]);
                }
            }
        }

        function renderList() {
            var listEl = container.querySelector('.ims-combo-list');
            if (!listEl) return;
            if (state.filtered.length === 0) {
                listEl.innerHTML = '<div class="ims-combo-no-results">No results found</div>';
                return;
            }
            listEl.innerHTML = state.filtered.map(function (item) {
                var val = getItemValue(item);
                var txt = getItemText(item);
                var isSelected = state.selected.indexOf(val) !== -1;
                var check = multiSelect ? '<input type="checkbox" ' + (isSelected ? 'checked' : '') + ' data-value="' + escapeHtml(val) + '" data-text="' + escapeHtml(txt) + '">' : '';
                return '<div class="ims-combo-item ' + (isSelected ? 'selected' : '') + '" data-value="' + escapeHtml(val) + '" data-text="' + escapeHtml(txt) + '">' + check + escapeHtml(txt) + '</div>';
            }).join('');
        }

        function filterList(query) {
            query = (query || '').toLowerCase().trim();
            if (!query) {
                state.filtered = data.slice();
            } else {
                state.filtered = data.filter(function (item) {
                    return getItemText(item).toLowerCase().indexOf(query) !== -1;
                });
            }
            renderList();
        }

        function toggleSelection(val, add) {
            var strVal = toStr(val);
            var idx = state.selected.indexOf(strVal);
            if (multiSelect) {
                if (add && idx === -1) state.selected.push(strVal);
                if (!add && idx !== -1) state.selected.splice(idx, 1);
            } else {
                state.selected = add ? [strVal] : [];
            }
            updateHiddenInput();
            renderDisplay();
            renderList();
        }

        function openPanel() {
            state.open = true;
            container.querySelector('.ims-combo-display').classList.add('open');
            container.querySelector('.ims-combo-panel').classList.add('open');
            filterList('');
            var searchEl = container.querySelector('.ims-combo-search');
            if (searchEl) { searchEl.value = ''; searchEl.focus(); }
        }

        function closePanel() {
            state.open = false;
            container.querySelector('.ims-combo-display').classList.remove('open');
            container.querySelector('.ims-combo-panel').classList.remove('open');
        }

        container.innerHTML = [
            '<div class="ims-combo-display" tabindex="0">',
            '  <span class="ims-combo-text ims-combo-placeholder">' + escapeHtml(placeholder) + '</span>',
            '  <span class="ims-combo-arrow">&#9662;</span>',
            '</div>',
            '<div class="ims-combo-panel">',
            '  <input type="text" class="ims-combo-search" placeholder="Type to search...">',
            '  <div class="ims-combo-list"></div>',
            '</div>'
        ].join('');

        container.appendChild(hiddenInput);

        container.querySelector('.ims-combo-display').addEventListener('click', function (e) {
            e.preventDefault();
            if (state.open) closePanel(); else openPanel();
        });

        container.querySelector('.ims-combo-search').addEventListener('input', function () {
            filterList(this.value);
        });

        container.querySelector('.ims-combo-search').addEventListener('keydown', function (e) {
            if (e.key === 'Escape') { e.preventDefault(); closePanel(); }
        });

        container.querySelector('.ims-combo-list').addEventListener('click', function (e) {
            e.stopPropagation();
            var item = e.target.closest('.ims-combo-item');
            var checkbox = e.target && e.target.type === 'checkbox' ? e.target : null;
            if (checkbox && item) {
                toggleSelection(checkbox.getAttribute('data-value'), checkbox.checked);
                if (!multiSelect) closePanel();
                return;
            }
            if (item && !item.classList.contains('ims-combo-no-results')) {
                var val = item.getAttribute('data-value');
                var isSelected = state.selected.indexOf(val) !== -1;
                toggleSelection(val, !isSelected);
                if (!multiSelect) closePanel();
            }
        });

        container.addEventListener('click', function (e) {
            var remove = e.target.classList && e.target.classList.contains('ims-combo-tag-remove');
            if (remove) {
                e.preventDefault();
                e.stopPropagation();
                var val = e.target.getAttribute('data-value');
                toggleSelection(val, false);
            }
        });

        document.addEventListener('click', function (e) {
            if (state.open && !container.contains(e.target)) closePanel();
        });

        updateHiddenInput();
        renderDisplay();
        renderList();

        return {
            getValue: function () { return multiSelect ? state.selected : (state.selected[0] || ''); },
            setValue: function (v) { state.selected = v != null ? (multiSelect ? (Array.isArray(v) ? v : [v]) : [v]) : []; updateHiddenInput(); renderDisplay(); renderList(); },
            getSelectedItems: getSelectedItems,
            open: openPanel,
            close: closePanel
        };
    }

    return { init: init };
})();