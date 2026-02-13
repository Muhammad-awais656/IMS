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
function isValidName(name) {
    // Trim leading and trailing spaces
    name = name.trim();

    // Regular expression: allows only alphabets (A–Z, a–z) and single spaces between words
    var nameRegex = /^[A-Za-z]+(?: [A-Za-z]+)*$/;

    return nameRegex.test(name);
}

function allowOnlyUrdu(input) {
    // Urdu Unicode range
    const urduRegex = /[^\u0600-\u06FF\s]/g;
    input.value = input.value.replace(urduRegex, '');
}