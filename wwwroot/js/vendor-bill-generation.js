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
    
    // Check if we're in edit mode and populate form
    if (typeof isEditMode !== 'undefined' && isEditMode) {
        populateEditForm();
    }
}

function initializeVendorComboBox() {
    var initialValue = $("#vendorSelect").val();
    
    $("#vendorSelect").kendoComboBox({
        placeholder: "Select vendor...",
        dataTextField: "text",
        dataValueField: "value",
        filter: "contains",
        minLength: 1,
        value: initialValue, // Set initial value if exists
        dataSource: {
            transport: {
                read: {
                    url: "/VendorBills/GetVendors",
                    dataType: "json"
                }
            },
            schema: {
                data: "data"
            }
        },
        change: function(e) {
            if (this.value()) {
                loadVendorData(this.value());
                // Clear validation styling when vendor is selected
                $("#vendorSelect").removeClass('is-invalid');
            }
        }
    });
    
    // If we have an initial value, load vendor data
    if (initialValue) {
        setTimeout(function() {
            loadVendorData(initialValue);
        }, 500);
    }
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
                    // Use VendorBills endpoint for products
                    url: "/VendorBills/GetProducts",
                    dataType: "json"
                }
            },
            schema: {
                data: "data"
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
                    url: "/VendorBills/GetOnlineAccounts",
                    dataType: "json"
                }
            },
            schema: {
                data: "data"
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
    $("#payNow").on("input", function() {
        calculateDueAmount();
        validateAccountBalance();
    });
    
    // Online account change
    $("#onlineAccountSelect").on("change", function() {
        // Clear validation styling
        $(this).removeClass('is-invalid');
        
        // Load account balance when account is selected
        var accountCombo = $(this).data("kendoComboBox");
        var accountId = accountCombo ? accountCombo.value() : null;
        
        if (accountId) {
            loadAccountBalance(accountId);
        } else {
            $("#accountBalance").val("");
            $("#billAmount").val("");
        }
    });
    
    // Total discount change
    $("#discountAmountTotal").on("input", calculateTotals);
    
    // Payment method change
    $("#paymentMethod").on("change", function() {
        var paymentMethod = $(this).val();
        if (paymentMethod === "Online") {
            $("#onlineAccountSection").show();
            $("#accountBalanceSection").show();
            $("#billAmountSection").show();
            // Load online accounts when Online is selected
            loadOnlineAccounts();
        } else {
            $("#onlineAccountSection").hide();
            $("#accountBalanceSection").hide();
            $("#billAmountSection").hide();
        }
        
        // Handle Pay Later option
        if (paymentMethod === "PayLater") {
            $("#payNow").val(0).prop("disabled", true);
        } else {
            $("#payNow").prop("disabled", false);
        }
        
        // Clear validation styling
        $(this).removeClass('is-invalid');
        $("#payNow").removeClass('is-invalid');
        $("#onlineAccountSelect").removeClass('is-invalid');
        
        calculateDueAmount();
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
            
            // Remove the item from the table and array
            billDetails.splice(index, 1);
            updateBillDetailsTable();
            calculateTotals();
            
            // Populate the form fields
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
            $("#discountAmount").val(item.lineDiscountAmount / item.quantity); // Convert back to per-unit discount
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

}

function initializeForm() {
    // Set today's date
    $("#billDate").val(new Date().toISOString().split('T')[0]);
    
    // Load next bill number
    loadNextBillNumber();
}

function loadVendorData(vendorId) {
    // Load previous due amount
    $.get("/VendorBills/GetPreviousDueAmount", { vendorId: vendorId })
        .done(function(data) {
            if (data.success) {
                $("#previousDue").val(data.previousDueAmount);
            }
        })
        .fail(function() {
            console.error("Error loading vendor data");
        });
    
    // Load next bill number for the selected vendor
    loadNextBillNumber(vendorId);
}

function loadProductSizes(productId) {
    // Use VendorBills endpoint for product sizes
    $.get("/VendorBills/GetProductSizes", { productId: productId })
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
    // Use VendorBills available stock endpoint
    $.get("/VendorBills/GetAvailableStock", { productId: productId })
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

function loadNextBillNumber(vendorId = null) {
    var url = "/VendorBills/GetNextBillNumber";
    if (vendorId) {
        url += "?vendorId=" + vendorId;
    }
    
    console.log("Loading next bill number for vendor:", vendorId, "URL:", url);
    
    $.get(url)
        .done(function(data) {
            console.log("Next bill number response:", data);
            if (data.success) {
                console.log("Setting bill number to:", data.billNumber);
                $("#billNumber").val(data.billNumber);
                console.log("Bill number field value after setting:", $("#billNumber").val());
            } else {
                console.error("Failed to get next bill number:", data.message);
            }
        })
        .fail(function(xhr, status, error) {
            console.error("Error loading next bill number:", error);
        });
}

function calculatePayableAmount() {
    var quantity = parseFloat($("#quantity").val()) || 0;
    var unitPrice = parseFloat($("#unitPrice").val()) || 0;
    var purchasePrice = parseFloat($("#purchasePrice").val()) || 0;
    var discountAmount = parseFloat($("#discountAmount").val()) || 0;
    
    var totalAmount = quantity * purchasePrice; // Use purchase price instead of unit price
    var totalDiscount = discountAmount * quantity; // Calculate total discount
    var payableAmount = totalAmount - totalDiscount; // Use total discount instead of per-unit discount
    
    $("#payableAmount").val(payableAmount.toFixed(2));
}

function calculateDueAmount() {
    var totalAmount = parseFloat($("#totalAmount").val()) || 0;
    var discountAmountTotal = parseFloat($("#discountAmountTotal").val()) || 0;
    var payNow = parseFloat($("#payNow").val()) || 0;
    
    // Calculate due amount for current bill only (not including previous dues)
    var netTotal = totalAmount - discountAmountTotal;
    var dueAmount = netTotal - payNow;
    
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
    
    // Update bill amount if online payment is selected
    var paymentMethod = $("#paymentMethod").val();
    if (paymentMethod === "Online") {
        updateBillAmount();
    }
    
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
        measuringUnitAbbreviation: selectedProductSize.measuringUnitAbbreviation || "",
        unitPrice: unitPrice,
        purchasePrice: purchasePrice,
        quantity: quantity,
        salePrice: unitPrice,
        lineDiscountAmount: discountAmount * quantity,
        payableAmount: payableAmount,
        productRangeId: selectedProductSize.productRangeId
    };

    if (editingIndex >= 0) {
        // Add the updated item back to the array
        billDetails.push(billDetail);
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
        row.append("<td>" + (item.measuringUnitAbbreviation || "") + "</td>");
        row.append("<td>" + item.productName + "</td>");
        row.append("<td>" + (item.lineDiscountAmount / item.quantity).toFixed(2) + "</td>");
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
    
    var actionType = $("#actionType").val();
    
    if (actionType === "saveAndPrint") {
        saveAndPrintBill();
    } else {
        saveBill();
    }
});

// Save Bill function
function saveBill() {
    if (!validateBillForm()) {
        return;
    }
    
    console.log('=== AJAX Save Bill called ===');
    showLoadingState();
    
    try {
        // Collect form data
        const formData = collectBillFormData('save');
        console.log('Form data collected:', formData);
        
        // Make AJAX call
        $.ajax({
            url: '/VendorBills/GenerateBill',
            type: 'POST',
            data: formData,
            dataType: 'json',
            headers: {
                'X-Requested-With': 'XMLHttpRequest'
            },
            success: function(response) {
                console.log('AJAX Save Bill success:', response);
                hideLoadingState();
                
                if (response.success) {
                    showSuccessMessage('Bill saved successfully!');
                    // Reset form after successful save
                    resetBillForm();
                } else {
                    showErrorMessage(response.message || 'Failed to save bill');
                }
            },
            error: function(xhr, status, error) {
                console.error('AJAX Save Bill error:', error);
                console.error('Response status:', xhr.status);
                console.error('Response text:', xhr.responseText);
                hideLoadingState();
                
                var errorMessage = 'Error saving bill: ' + error;
                if (xhr.responseJSON && xhr.responseJSON.message) {
                    errorMessage = xhr.responseJSON.message;
                }
                showErrorMessage(errorMessage);
            }
        });
    } catch (error) {
        console.error('Error in saveBill function:', error);
        hideLoadingState();
        showErrorMessage('Error preparing bill data: ' + error.message);
    }
}

// Save and Print Bill function
function saveAndPrintBill() {
    if (!validateBillForm()) {
        return;
    }
    
    console.log('=== AJAX Save and Print Bill called ===');
    showLoadingState();
    
    try {
        // Collect form data
        const formData = collectBillFormData('saveAndPrint');
        console.log('Form data collected:', formData);
        
        // Make AJAX call
        $.ajax({
            url: '/VendorBills/GenerateBill',
            type: 'POST',
            data: formData,
            dataType: 'json',
            headers: {
                'X-Requested-With': 'XMLHttpRequest'
            },
            success: function(response) {
                console.log('AJAX Save and Print Bill success:', response);
                hideLoadingState();
                
                if (response.success) {
                    showSuccessMessage('Bill saved successfully!');
                    // Reset form after successful save
                    resetBillForm();
                    
                    // Open print windows for both vendor and merchant copies in separate tabs
                    if (response.billId) {
                        // Open vendor copy in new tab and print
                        const vendorPrintUrl = '/VendorBills/PrintReceipt/' + response.billId + '?print=true&merchantCopy=false';
                        const vendorWindow = window.open(vendorPrintUrl, '_blank');
                        
                        // Open merchant copy in another new tab after a delay
                        setTimeout(function() {
                            const merchantPrintUrl = '/VendorBills/PrintReceipt/' + response.billId + '?print=true&merchantCopy=true';
                            const merchantWindow = window.open(merchantPrintUrl, '_blank');
                            
                            // Show notification about both copies being opened
                            showSuccessMessage('Both Vendor Copy and Merchant Copy have been opened in separate tabs!');
                        }, 1000); // 1 second delay between tabs
                    }
                } else {
                    showErrorMessage(response.message || 'Failed to save bill');
                }
            },
            error: function(xhr, status, error) {
                console.error('AJAX Save and Print Bill error:', error);
                console.error('Response status:', xhr.status);
                console.error('Response text:', xhr.responseText);
                hideLoadingState();
                
                var errorMessage = 'Error saving bill: ' + error;
                if (xhr.responseJSON && xhr.responseJSON.message) {
                    errorMessage = xhr.responseJSON.message;
                }
                showErrorMessage(errorMessage);
            }
        });
    } catch (error) {
        console.error('Error in saveAndPrintBill function:', error);
        hideLoadingState();
        showErrorMessage('Error preparing bill data: ' + error.message);
    }
}

// Function to load online accounts
function loadOnlineAccounts() {
    const onlineAccountCombo = $("#onlineAccountSelect").data("kendoComboBox");
    if (onlineAccountCombo) {
        console.log("Loading online accounts from server...");
        fetch("/VendorBills/GetOnlineAccounts")
            .then(response => {
                console.log("Online accounts response status:", response.status);
                if (!response.ok) {
                    throw new Error(`HTTP error! status: ${response.status}`);
                }
                return response.json();
            })
            .then(data => {
                console.log("Online accounts data received:", data);
                if (data && data.length > 0) {
                    onlineAccountCombo.dataSource.data(data);
                    onlineAccountCombo.enable(true);
                    onlineAccountCombo.refresh();
                    console.log("Online accounts loaded successfully");
                } else {
                    console.log("No online accounts found");
                    onlineAccountCombo.dataSource.data([]);
                    onlineAccountCombo.enable(false);
                }
            })
            .catch(error => {
                console.error("Error loading online accounts:", error);
                onlineAccountCombo.dataSource.data([]);
                onlineAccountCombo.enable(false);
            });
    }
}

// Function to load account balance
function loadAccountBalance(accountId) {
    console.log("Loading account balance for account:", accountId);
    fetch(`/VendorBills/GetAccountBalance?accountId=${accountId}`)
        .then(response => {
            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }
            return response.json();
        })
        .then(data => {
            console.log("Account balance data received:", data);
            if (data.success) {
                $("#accountBalance").val(data.balance.toFixed(2));
                updateBillAmount();
            } else {
                console.error("Error loading account balance:", data.message);
                $("#accountBalance").val("");
            }
        })
        .catch(error => {
            console.error("Error loading account balance:", error);
            $("#accountBalance").val("");
        });
}

// Function to update bill amount
function updateBillAmount() {
    var totalAmount = parseFloat($("#totalAmount").val()) || 0;
    var discountAmountTotal = parseFloat($("#discountAmountTotal").val()) || 0;
    var payNow = parseFloat($("#payNow").val()) || 0;
    
    var billAmount = totalAmount - discountAmountTotal;
    $("#billAmount").val(billAmount.toFixed(2));
}

// Function to validate account balance
function validateAccountBalance() {
    var paymentMethod = $("#paymentMethod").val();
    
    if (paymentMethod === "Online") {
        var accountBalance = parseFloat($("#accountBalance").val()) || 0;
        var payNow = parseFloat($("#payNow").val()) || 0;
        
        // Clear previous validation styling
        $("#payNow").removeClass('is-invalid');
        $("#accountBalance").removeClass('is-invalid');
        
        if (accountBalance <= 0) {
            $("#accountBalance").addClass('is-invalid');
            showWarningMessage("Account balance is insufficient. Available balance: $" + accountBalance.toFixed(2));
            return false;
        }
        
        if (payNow > accountBalance) {
            $("#payNow").addClass('is-invalid');
            showWarningMessage("Payment amount exceeds available account balance. Available balance: $" + accountBalance.toFixed(2) + ", Payment amount: $" + payNow.toFixed(2));
            return false;
        }
        
        // Clear validation styling if balance is sufficient
        $("#payNow").removeClass('is-invalid');
        $("#accountBalance").removeClass('is-invalid');
    }
    
    return true;
}

// Function to show warning message (toaster-style)
function showWarningMessage(message) {
    // Remove existing warning messages
    $('.alert-warning').remove();
    
    // Create warning alert
    var alertHtml = '<div class="alert alert-warning alert-dismissible fade show" role="alert" style="position: fixed; top: 20px; right: 20px; z-index: 9999; min-width: 300px;">' +
        '<i class="fa-solid fa-exclamation-triangle me-2"></i>' +
        message +
        '<button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>' +
        '</div>';
    
    $('body').append(alertHtml);
    
    // Auto-hide after 5 seconds
    setTimeout(function() {
        $('.alert-warning').fadeOut();
    }, 5000);
}

// Function to validate bill form
function validateBillForm() {
    // Validate vendor selection
    var vendorCombo = $("#vendorSelect").data("kendoComboBox");
    var vendorId = vendorCombo ? vendorCombo.value() : null;
    
    if (!vendorId || vendorId === '') {
        showWarningMessage("Please select a vendor from the dropdown.");
        $("#vendorSelect").addClass('is-invalid');
        return false;
    }
    
    // Clear vendor validation styling if vendor is selected
    $("#vendorSelect").removeClass('is-invalid');
    
    if (billDetails.length === 0) {
        alert("Please add at least one product to the bill");
        return false;
    }
    
    // Validate Pay Now based on Payment Method
    var paymentMethod = $("#paymentMethod").val();
    var payNow = parseFloat($("#payNow").val()) || 0;
    
    if (paymentMethod !== "PayLater" && payNow <= 0) {
        alert("Pay Now amount is required when payment method is not 'Pay Later'.");
        return false;
    }
    
    // Validate Online Account selection for Online payment method
    if (paymentMethod === "Online") {
        var onlineAccountCombo = $("#onlineAccountSelect").data("kendoComboBox");
        var onlineAccountId = onlineAccountCombo ? onlineAccountCombo.value() : null;
        
        if (!onlineAccountId || onlineAccountId === '') {
            alert("Please select an online account for online payment.");
            return false;
        }
        
        // Validate account balance
        var accountBalance = parseFloat($("#accountBalance").val()) || 0;
        var payNow = parseFloat($("#payNow").val()) || 0;
        
        if (accountBalance <= 0) {
            alert("Account balance is insufficient. Available balance: $" + accountBalance.toFixed(2));
            return false;
        }
        
        if (payNow > accountBalance) {
            alert("Payment amount exceeds available account balance. Available balance: $" + accountBalance.toFixed(2) + ", Payment amount: $" + payNow.toFixed(2));
            return false;
        }
    }
    
    return true;
}

// Function to collect form data for AJAX submission
function collectBillFormData(actionType) {
    // Get anti-forgery token
    var token = $('input[name="__RequestVerificationToken"]').val();
    
    var formData = {
        __RequestVerificationToken: token,
        VendorId: parseInt($("#vendorSelect").val()) || 0,
        BillNumber: parseInt($("#billNumber").val()) || 0,
        BillDate: $("#billDate").val(),
        TotalAmount: parseFloat($("#totalAmount").val()) || 0,
        DiscountAmount: parseFloat($("#discountAmountTotal").val()) || 0,
        PaidAmount: parseFloat($("#payNow").val()) || 0,
        DueAmount: parseFloat($("#dueAmount").val()) || 0,
        PreviousDue: parseFloat($("#previousDue").val()) || 0,
        PayNow: parseFloat($("#payNow").val()) || 0,
        Description: $("#description").val() || "",
        PaymentMethod: $("#paymentMethod").val(),
        OnlineAccountId: $("#onlineAccountSelect").val() ? parseInt($("#onlineAccountSelect").val()) : null,
        ActionType: actionType,
        BillDetails: []
    };
    
    // Add bill details
    billDetails.forEach(function(item) {
        formData.BillDetails.push({
            ProductId: parseInt(item.productId),
            ProductRangeId: parseInt(item.productRangeId),
            ProductSize: item.measuringUnitAbbreviation || "",
            UnitPrice: parseFloat(item.unitPrice),
            PurchasePrice: parseFloat(item.purchasePrice),
            Quantity: parseInt(item.quantity),
            SalePrice: parseFloat(item.salePrice),
            LineDiscountAmount: parseFloat(item.lineDiscountAmount),
            PayableAmount: parseFloat(item.payableAmount)
        });
    });
    
    // Debug logging
    console.log('Form data being sent:', JSON.stringify(formData, null, 2));
    console.log('Bill details count:', formData.BillDetails.length);
    console.log('VendorId:', formData.VendorId);
    console.log('BillNumber:', formData.BillNumber);
    console.log('PaymentMethod:', formData.PaymentMethod);
    
    return formData;
}

// Function to reset bill form
function resetBillForm() {
    // Clear bill details
    billDetails = [];
    updateBillDetailsTable();
    
    // Reset form fields
    $("#vendorSelect").data("kendoComboBox").value("");
    $("#billDate").val(new Date().toISOString().split('T')[0]);
    $("#totalAmount").val("0");
    $("#discountAmountTotal").val("0");
    $("#payNow").val("0");
    $("#dueAmount").val("0");
    $("#previousDue").val("0");
    $("#description").val("");
    $("#receivedAmount").val("");
    $("#paymentMethod").val("Cash");
    $("#onlineAccountSelect").data("kendoComboBox").value("");
    
    // Hide online account sections
    $("#onlineAccountSection").hide();
    $("#accountBalanceSection").hide();
    $("#billAmountSection").hide();
    
    // Reset product fields
    resetProductFields();
    
    // Reset action type
    $("#actionType").val("save");
    
    // Reload next bill number (without vendor ID since vendor is cleared)
    console.log("Resetting form and loading next bill number...");
    loadNextBillNumber();
}

// Function to show loading state
function showLoadingState() {
    $("#saveButton").prop("disabled", true).html('<i class="fa-solid fa-spinner fa-spin me-1"></i>Saving...');
    $("#saveAndPrintButton").prop("disabled", true).html('<i class="fa-solid fa-spinner fa-spin me-1"></i>Saving...');
}

// Function to hide loading state
function hideLoadingState() {
    $("#saveButton").prop("disabled", false).html('<i class="fa-solid fa-save me-1"></i>Save');
    $("#saveAndPrintButton").prop("disabled", false).html('<i class="fa-solid fa-print me-1"></i>Save & Print');
}

// Function to show success message
function showSuccessMessage(message) {
    // Remove existing success messages
    $('.alert-success').remove();
    
    // Create success alert
    var alertHtml = '<div class="alert alert-success alert-dismissible fade show" role="alert" style="position: fixed; top: 20px; right: 20px; z-index: 9999; min-width: 300px;">' +
        '<i class="fa-solid fa-check-circle me-2"></i>' +
        message +
        '<button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>' +
        '</div>';
    
    $('body').append(alertHtml);
    
    // Auto-hide after 3 seconds
    setTimeout(function() {
        $('.alert-success').fadeOut();
    }, 3000);
}

// Function to show error message
function showErrorMessage(message) {
    // Remove existing error messages
    $('.alert-danger').remove();
    
    // Create error alert
    var alertHtml = '<div class="alert alert-danger alert-dismissible fade show" role="alert" style="position: fixed; top: 20px; right: 20px; z-index: 9999; min-width: 300px;">' +
        '<i class="fa-solid fa-exclamation-circle me-2"></i>' +
        message +
        '<button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>' +
        '</div>';
    
    $('body').append(alertHtml);
    
    // Auto-hide after 5 seconds
    setTimeout(function() {
        $('.alert-danger').fadeOut();
    }, 5000);
}

// Function to populate form fields in edit mode
function populateEditForm() {
    console.log('Populating edit form with existing data...');
    console.log('isEditMode:', isEditMode);
    console.log('billDetailsData:', billDetailsData);
    console.log('billDetailsData type:', typeof billDetailsData);
    console.log('billDetailsData length:', billDetailsData ? billDetailsData.length : 'undefined');
    
    // Populate basic form fields
    if ($("#vendorSelect").val()) {
        var vendorCombo = $("#vendorSelect").data("kendoComboBox");
        if (vendorCombo) {
            vendorCombo.value($("#vendorSelect").val());
            // Load vendor data after setting value
            setTimeout(function() {
                loadVendorData($("#vendorSelect").val());
            }, 500);
        }
    }
    
    // Populate bill details from server-side data
    if (typeof billDetailsData !== 'undefined' && billDetailsData && billDetailsData.length > 0) {
        console.log('Loading bill details:', billDetailsData);
        billDetails = billDetailsData.map(function(item) {
            console.log('Processing item:', item);
            return {
                productId: item.ProductId,
                productRangeId: item.ProductRangeId,
                productName: item.ProductName || '', // This might be undefined
                productSize: item.ProductSize || '',
                measuringUnitAbbreviation: item.ProductSize || '',
                unitPrice: item.UnitPrice,
                purchasePrice: item.PurchasePrice,
                quantity: item.Quantity,
                salePrice: item.SalePrice,
                lineDiscountAmount: item.LineDiscountAmount,
                payableAmount: item.PayableAmount
            };
        });
        
        console.log('Mapped billDetails:', billDetails);
        
        // Update the bill details table
        updateBillDetailsTable();
        calculateTotals();
    } else {
        console.log('No bill details data found or data is empty');
    }
    
    console.log('Edit form populated successfully');
}
