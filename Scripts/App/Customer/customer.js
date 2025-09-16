var Customers = (function () {

    var messages = {};
    var baseUrl = '';
    var grid = null;
    var pager = null;
    var txtSearchCustomer = null;
    var urlGetData = baseUrl + '/Controllers/Customer/GetCustomers';

    var init = function () {
        debugger;
        grid = $('#customerGrid');
        pager = $('#gridPager');
        txtSearchCustomer = $('#txtSearchCustomer');
        Customers.bindPageEvents();
        Customers.loadData();

    };

    var bindPageEvents = function () {
        $('#btnAddCustomer').on('click', function () {
            Customers.openAddEditModal(null);
        }
        );
        $txtSearchCustomer.off('keyup', function () {
            var shouldSearch = $(this).val().length >= 3 || $(this).val().length === 0;
            if (shouldSearch) {
                Customers.SearchGrid();
            }
        });


    });



        $(document).ready(function () {
            $("#customerGrid").jqGrid({
                url: '@Url.Action("GetCustomers", "Customer")',
                datatype: 'json',
                mtype: 'GET',
                colNames: ['ID', 'Name', 'Email', 'Phone', 'Address', 'Actions'],
                colModel: [
                    { name: 'id', index: 'Id', width: 60, sorttype: 'int', key: true, hidden: true },
                    { name: 'Name', index: 'Name', width: 150, editable: true, search: true, stype: 'text', searchoptions: { sopt: ['cn'] } },
                    { name: 'Email', index: 'Email', width: 200, editable: true },
                    { name: 'Phone', index: 'Phone', width: 150, editable: true, search: true, stype: 'text', searchoptions: { sopt: ['cn'] } },
                    { name: 'Address', index: 'Address', width: 200, editable: true, search: true, stype: 'text', searchoptions: { sopt: ['cn'] } },
                    {
                        name: 'act', index: 'act', width: 100, sortable: false, formatter: 'actions',
                        formatoptions: {
                            keys: true,
                            editbutton: true,
                            delbutton: true,
                            editformbutton: false,
                            onEdit: function (rowid) {
                                var rowData = $("#customerGrid").jqGrid('getRowData', rowid);
                                $('#id').val(rowid);
                                $('#name').val(rowData.Name);
                                $('#email').val(rowData.Email);
                                $('#phone').val(rowData.Phone);
                                $('#address').val(rowData.Address);
                            },
                            onSuccess: function (response) {
                                return true;
                            },
                            onError: function (rowid, response) {
                                alert('Error: ' + response.responseText);
                                return false;
                            }
                        }
                    }
                ],
                pager: '#gridPager',
                rowNum: 10,
                rowList: [10, 20, 30],
                sortname: 'Id',
                sortorder: 'asc',
                viewrecords: true,
                height: 'auto',
                caption: 'Customer List',
                jsonReader: {
                    root: 'rows',
                    page: 'page',
                    total: 'total',
                    records: 'records',
                    repeatitems: true,
                    id: 'id'
                },
                loadError: function (xhr, status, error) {
                    alert('Error loading data: ' + error);
                }
            });

        // Enable search toolbar
        $("#customerGrid").jqGrid('filterToolbar', {
            stringResult: true,
        searchOnEnter: true,
        defaultSearch: 'cn' // Case-insensitive contains
            });

        // Navigation bar for CRUD operations
        $("#customerGrid").jqGrid('navGrid', '#gridPager', {
            edit: false,
        add: false,
        del: false,
        search: false, // Disable navGrid search to use toolbar search
        refresh: true
            });

        // Form submission for add/edit
        $('#customerForm').submit(function (e) {
            e.preventDefault();
        var formData = {
            id: $('#id').val(),
        Name: $('#name').val(),
        Email: $('#email').val(),
        Phone: $('#phone').val(),
        Address: $('#address').val(),
        oper: $('#id').val() ? 'edit' : 'add'
                };

        $.ajax({
            url: '@Url.Action("EditCustomer", "Customer")',
        type: 'POST',
        data: formData,
        success: function (response) {
                        if (response.success) {
            $("#customerGrid").trigger('reloadGrid');
        clearForm();
        alert(response.message);
                        } else {
            alert('Error: ' + response.message);
                        }
                    },
        error: function (xhr, status, error) {
            alert('Error saving customer: ' + error);
                    }
                });
            });
        });

        function clearForm() {
            $('#customerForm')[0].reset();
        $('#id').val('');
        }
   


    });
