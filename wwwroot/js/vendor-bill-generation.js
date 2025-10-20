// Vendor Bill Generation JavaScript
let billDetails = [];
let currentProductId = null;
let currentProductRangeId = null;
let editingIndex = -1;

function initializeVendorBillGeneration() {
    // Initialize Kendo UI components
    initializeVendorComboBox();
    initializeProductComboBox();
    initializeProductSizeComboBox();
    initializeOnlineAccountComboBox();
    
    // Set up event handlers
    setupEventHandlers();
    
    // Initialize form
    initializeForm();
}

function initializeVendorComboBox() {
    $("#vendorSelect").kendoComboBox({
        placeholder: "Select vendor...",
        dataTextField: "text",
        dataValueField: "value",
        filter: "contains",
        minLength: 1,
        dataSource: {
            transport: {
                read: {
                    url: "/Vendor/GetVendors",
                    dataType: "json"
                }
            }
        },
        change: function(e) {
            if (this.value()) {
                loadVendorData(this.value());
            }
        }
    });
}

function initializeProductComboBox() {
    $("#productSelect").kendoComboBox({
        placeholder: "Select product...",
        dataTextField: "text",
        dataValueField: "value",
        filter: "contains",
        minLength: 1,
        dataSource: {
            transport: {
                read: {
                    // Use Product endpoint to also receive product code
                    url: "/Product/GetProducts",
                    dataType: "json"
                }
            }
        },
        change: function(e) {
            if (this.value()) {
                currentProductId = this.value();
                loadProductSizes(this.value());
                loadProductStock(this.value());
            }
        }
    });
}

function initializeProductSizeComboBox() {
    $("#productSizeSelect").kendoComboBox({
        placeholder: "Select product size...",
        dataTextField: "text",
        dataValueField: "value",
        filter: "contains",
        minLength: 1,
        dataSource: {
            data: []
        },
        change: function(e) {
            if (this.value()) {
                currentProductRangeId = this.value();
                loadProductSizeData(this.value());
            }
        }
    });
}

function initializeOnlineAccountComboBox() {
    $("#onlineAccountSelect").kendoComboBox({
        placeholder: "Select online account...",
        dataTextField: "text",
        dataValueField: "value",
        filter: "contains",
        minLength: 1,
        dataSource: {
            transport: {
                read: {
                    url: "/Vendor/GetOnlineAccounts",
                    dataType: "json"
                }
            }
        }
    });
}

function setupEventHandlers() {
    // Quantity change
    $("#quantity").on("input", calculatePayableAmount);
    
    // Unit price change
    $("#unitPrice").on("input", calculatePayableAmount);
    
    // Purchase price change
    $("#purchasePrice").on("input", calculatePayableAmount);
    
    // Discount amount change
    $("#discountAmount").on("input", calculatePayableAmount);
    
    // Pay now change
    $("#payNow").on("input", calculateDueAmount);
    
    // Total discount change
    $("#discountAmountTotal").on("input", calculateTotals);
    
    // Payment method change
    $("#paymentMethod").on("change", function() {
        if ($(this).val() === "Online") {
            $("#onlineAccountSection").show();
        } else {
            $("#onlineAccountSection").hide();
        }
    });
    
    // Add to table button
    $("#addToTable").on("click", addProductToTable);
    
    // Reset fields button
    $("#resetFields").on("click", resetProductFields);
    
    // Save button
    $("#saveButton").on("click", function() {
        $("#actionType").val("save");
    });
    
    // Save and print button
    $("#saveAndPrintButton").on("click", function() {
        $("#actionType").val("saveAndPrint");
    });
    
    // Keyboard shortcuts
    $(document).on("keydown", function(e) {
        if (e.ctrlKey && e.key === "Enter") {
            e.preventDefault();
            addProductToTable();
        } else if (e.key === "Escape") {
            e.preventDefault();
            resetProductFields();
        }
    });

    // Delegate edit/remove clicks on table
    $(document).on("click", ".edit-row", function() {
        const index = $(this).data("index");
        if (index != null && billDetails[index]) {
            const item = billDetails[index];
            editingIndex = index;
            const productCb = $("#productSelect").data("kendoComboBox");
            const sizeCb = $("#productSizeSelect").data("kendoComboBox");
            productCb.value(item.productId);
            loadProductSizes(item.productId);
            setTimeout(function(){
                sizeCb.value(item.productRangeId);
            }, 200);
            $("#quantity").val(item.quantity);
            $("#unitPrice").val(item.unitPrice);
            $("#purchasePrice").val(item.purchasePrice);
            $("#discountAmount").val(item.lineDiscountAmount);
            $("#payableAmount").val(item.payableAmount);
            $("#addToTable").text("Update Item");
        }
    });

    $(document).on("click", ".remove-row", function() {
        const index = $(this).data("index");
        if (index != null) {
            billDetails.splice(index, 1);
            updateBillDetailsTable();
            calculateTotals();
        }
    });

    // Pay later toggle
    $(document).on("change", "#payLater", function(){
        if (this.checked) {
            $("#payNow").val(0).prop("disabled", true);
        } else {
            $("#payNow").prop("disabled", false);
        }
        calculateDueAmount();
    });
}

function initializeForm() {
    // Set today's date
    $("#billDate").val(new Date().toISOString().split('T')[0]);
    
    // Load next bill number
    loadNextBillNumber();
}

function loadVendorData(vendorId) {
    // Load previous due amount
    $.get("/Vendor/GetPreviousDueAmount", { vendorId: vendorId })
        .done(function(data) {
            if (data.success) {
                $("#previousDue").val(data.previousDueAmount);
            }
        })
        .fail(function() {
            console.error("Error loading vendor data");
        });
}

function loadProductSizes(productId) {
    // Use Sales endpoint which returns standardized product size data
    $.get("/Sales/GetProductSizes", { productId: productId })
        .done(function(data) {
            var productSizeComboBox = $("#productSizeSelect").data("kendoComboBox");
            // Ensure proper Kendo DataSource binding
            var ds = new kendo.data.DataSource({ data: data });
            productSizeComboBox.setDataSource(ds);
            productSizeComboBox.refresh();
        })
        .fail(function() {
            console.error("Error loading product sizes");
        });
}

function loadProductSizeData(productRangeId) {
    var productSizeComboBox = $("#productSizeSelect").data("kendoComboBox");
    var selectedItem = productSizeComboBox.dataItem();
    
    if (selectedItem) {
        $("#unitPrice").val(selectedItem.unitPrice);
        $("#purchasePrice").val(selectedItem.unitPrice); // Default to unit price
        calculatePayableAmount();
    }
}

function loadProductStock(productId) {
    // Reuse Sales available stock endpoint (same stock table)
    $.get("/Sales/GetAvailableStock", { productId: productId })
        .done(function(data) {
            if (data.success) {
                // You can display stock information if needed
                console.log("Available stock:", data.availableQuantity);
            }
        })
        .fail(function() {
            console.error("Error loading product stock");
        });
}

function loadNextBillNumber() {
    $.get("/Vendor/GetNextBillNumber")
        .done(function(data) {
            if (data.success) {
                $("#billNumber").val(data.billNumber);
            }
        })
        .fail(function() {
            console.error("Error loading next bill number");
        });
}

function calculatePayableAmount() {
    var quantity = parseFloat($("#quantity").val()) || 0;
    var unitPrice = parseFloat($("#unitPrice").val()) || 0;
    var purchasePrice = parseFloat($("#purchasePrice").val()) || 0;
    var discountAmount = parseFloat($("#discountAmount").val()) || 0;
    
    var totalAmount = quantity * unitPrice;
    var payableAmount = totalAmount - discountAmount;
    
    $("#payableAmount").val(payableAmount.toFixed(2));
}

function calculateDueAmount() {
    var totalAmount = parseFloat($("#totalAmount").val()) || 0;
    var discountAmountTotal = parseFloat($("#discountAmountTotal").val()) || 0;
    var payNow = parseFloat($("#payNow").val()) || 0;
    var previousDue = parseFloat($("#previousDue").val()) || 0;
    
    var netTotal = totalAmount - discountAmountTotal;
    var totalPayable = netTotal + previousDue;
    var dueAmount = totalPayable - payNow;
    
    $("#dueAmount").val(dueAmount.toFixed(2));
    $("#receivedAmount").val(payNow);
}

function calculateTotals() {
    var totalAmount = 0;
    var totalDiscount = 0;
    
    billDetails.forEach(function(item) {
        totalAmount += item.payableAmount;
        totalDiscount += item.lineDiscountAmount;
    });
    
    $("#totalAmount").val(totalAmount.toFixed(2));
    $("#discountAmountTotal").val(totalDiscount.toFixed(2));
    
    calculateDueAmount();
}

function addProductToTable() {
    var productComboBox = $("#productSelect").data("kendoComboBox");
    var productSizeComboBox = $("#productSizeSelect").data("kendoComboBox");
    
    if (!productComboBox.value()) {
        alert("Please select a product");
        return;
    }
    
    if (!productSizeComboBox.value()) {
        alert("Please select a product size");
        return;
    }
    
    var selectedProduct = productComboBox.dataItem();
    var selectedProductSize = productSizeComboBox.dataItem();
    
    var quantity = parseFloat($("#quantity").val()) || 0;
    var unitPrice = parseFloat($("#unitPrice").val()) || 0;
    var purchasePrice = parseFloat($("#purchasePrice").val()) || 0;
    var discountAmount = parseFloat($("#discountAmount").val()) || 0;
    var payableAmount = parseFloat($("#payableAmount").val()) || 0;
    
    if (quantity <= 0) {
        alert("Please enter a valid quantity");
        return;
    }
    
    var billDetail = {
        productId: parseInt(selectedProduct.value),
        productName: selectedProduct.text,
        productCode: selectedProduct.code || "",
        unitPrice: unitPrice,
        purchasePrice: purchasePrice,
        quantity: quantity,
        salePrice: unitPrice,
        lineDiscountAmount: discountAmount,
        payableAmount: payableAmount,
        productRangeId: selectedProductSize.productRangeId
    };

    if (editingIndex >= 0) {
        billDetails[editingIndex] = billDetail;
        editingIndex = -1;
        $("#addToTable").text("Add to Bill");
    } else {
        billDetails.push(billDetail);
    }
    updateBillDetailsTable();
    calculateTotals();
    resetProductFields();
}

function updateBillDetailsTable() {
    var tbody = $("#billDetailsBody");
    tbody.empty();
    
    billDetails.forEach(function(item, index) {
        var row = $("<tr>");
        row.append("<td>" + (item.productCode || "") + "</td>");
        row.append("<td>" + item.productName + "</td>");
        row.append("<td>" + item.lineDiscountAmount.toFixed(2) + "</td>");
        row.append("<td>" + item.unitPrice.toFixed(2) + "</td>");
        row.append("<td>" + item.purchasePrice.toFixed(2) + "</td>");
        row.append("<td>" + item.quantity + "</td>");
        row.append("<td>" + item.lineDiscountAmount.toFixed(2) + "</td>");
        row.append("<td>" + item.payableAmount.toFixed(2) + "</td>");
        row.append("<td>"
            + "<button type='button' class='btn btn-sm btn-warning me-1 edit-row' data-index='" + index + "' title='Edit'><i class='fa-solid fa-edit'></i></button>"
            + "<button type='button' class='btn btn-sm btn-danger remove-row' data-index='" + index + "' title='Remove'><i class='fa-solid fa-trash'></i></button>"
            + "</td>");
        tbody.append(row);
    });
}

function removeBillDetail(index) {
    billDetails.splice(index, 1);
    updateBillDetailsTable();
    calculateTotals();
}

function resetProductFields() {
    $("#productSelect").data("kendoComboBox").value("");
    $("#productSizeSelect").data("kendoComboBox").value("");
    $("#quantity").val("1");
    $("#unitPrice").val("0");
    $("#purchasePrice").val("0");
    $("#discountAmount").val("0");
    $("#payableAmount").val("0");
    
    currentProductId = null;
    currentProductRangeId = null;
}

// Form submission
$("#generateBillForm").on("submit", function(e) {
    e.preventDefault();
    
    if (billDetails.length === 0) {
        alert("Please add at least one product to the bill");
        return;
    }
    
    // Add bill details to form (serialize as BillDetails[index].Field for MVC binding)
    // First remove any previous hidden inputs we added
    $(this).find("input[name^='BillDetails[']").remove();
    billDetails.forEach(function(item, idx){
        $("#generateBillForm").append($('<input>', {type:'hidden', name:`BillDetails[${idx}].ProductId`, value:item.productId}));
        $("#generateBillForm").append($('<input>', {type:'hidden', name:`BillDetails[${idx}].ProductRangeId`, value:item.productRangeId}));
        $("#generateBillForm").append($('<input>', {type:'hidden', name:`BillDetails[${idx}].UnitPrice`, value:item.unitPrice}));
        $("#generateBillForm").append($('<input>', {type:'hidden', name:`BillDetails[${idx}].PurchasePrice`, value:item.purchasePrice}));
        $("#generateBillForm").append($('<input>', {type:'hidden', name:`BillDetails[${idx}].Quantity`, value:item.quantity}));
        $("#generateBillForm").append($('<input>', {type:'hidden', name:`BillDetails[${idx}].SalePrice`, value:item.salePrice}));
        $("#generateBillForm").append($('<input>', {type:'hidden', name:`BillDetails[${idx}].LineDiscountAmount`, value:item.lineDiscountAmount}));
        $("#generateBillForm").append($('<input>', {type:'hidden', name:`BillDetails[${idx}].PayableAmount`, value:item.payableAmount}));
    });
    
    // Submit form
    this.submit();
});
