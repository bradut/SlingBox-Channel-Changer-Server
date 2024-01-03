/*********************************************
 * Ensure the channel number text contains a dot,
 * given this is an analogue channel
 ***********************************************/
function setAnalogueChannelNumber() {
    const digitsInput = document.getElementsByName("Digits")[0];
    const digitsValue = digitsInput.value.trim();

    // if user clicked on the other buttons: Ch+, Ch-, Last, the text box will be empty:
    if (isNullOrUndefinedOrEmpty(digitsValue)) {
        return true;
    }

    if (!isNaN(digitsValue) && !digitsValue.includes(".")) {
        digitsInput.value = digitsValue + ".";
    }

    // The "return true" statement at the end of the function ensures that the form is submitted.
    return true;
}

class SlingBoxStatus {
    static _isAnalogue = false;
    static get isAnalogue() {
        return this._isAnalogue;
    }
    static set isAnalogue(value) {
        this._isAnalogue = value;
    }
}


function isNullOrUndefinedOrEmpty(someObject) {
    if (isHtmlElement(someObject)) {
        return false;
    }
    if (isHtmlNode(someObject)) {
        return false;
    }

    return someObject === null || someObject === undefined || someObject.toString().length === 0;
}

// https://stackoverflow.com/questions/384286/how-do-you-check-if-a-javascript-object-is-a-dom-object
//Returns true if it is a DOM node
function isHtmlNode(someObject) {
    return (
        typeof Node === "object" ? someObject instanceof Node :
            someObject && typeof someObject === "object" && typeof someObject.nodeType === "number" && typeof someObject.nodeName === "string"
    );
}

//Returns true if it is a DOM element    
function isHtmlElement(someObject) {
    return (
        typeof HTMLElement === "object"
            ? someObject instanceof HTMLElement
            : someObject && typeof someObject === "object" && true && someObject.nodeType === 1 && typeof someObject.nodeName === "string"
    );
}


// Add a time stamp will prevent caching
function appendTimeStampToUrl(url) {
    if (!url) {
        return "";
    }

    return url.includes("?")
        ? `${url}&timeStamp=${Date.now()}`
        : `${url}?timeStamp=${Date.now()}`;
}

/**********************************************
 * Get Slingbox Server info - for debugging
 ***********************************************/
function displayServerInfo() {
    const displayDoc = document.getElementById("displayDebugInfoHere");
    if (isNullOrUndefinedOrEmpty(displayDoc))
        return;

    displayDoc.innerHTML = "";
    displayDoc.innerHTML += "<style>table, th, tr, td {border: 1px solid; text-align: center;}</style > ";
    displayDoc.innerHTML += "Page location is " + window.location.href + "<br />"; // the URL of the current page. Ex: http://192.168.1.10:65432/Remote/sling1

    var table = document.createElement("table");
    table.setAttribute("border", "1");
    table.setAttribute("width", "100%");
    table.innerHTML += "<tr><th>Property</th><th>Value</th><th>Property</th><th>Value</th></tr>";
    table.innerHTML += "<tr><td>Protocol: </td><td>" + window.location.protocol + "</td>  <td>Hostname: </td><td>" + window.location.hostname + "</td></tr>";
    table.innerHTML += "<tr><td> Port:</td><td>" + window.location.port + "</td>  <td>Pathname: </td><td>" + window.location.pathname + "</td></tr>";
    displayDoc.appendChild(table);
}



class DataAccess {
    static readSlingBoxStatus(slingBoxName, jsonFileName) {
        return fetch(jsonFileName)
            .then(response => response.json())
            .then(jsonData => {
                const serverStatus = {
                    tvGuideUrl: jsonData.tvGuideUrl,
                    urlBase: jsonData.urlBase,
                    slingRemoteControlUrl: jsonData.slingRemoteControlServiceUrl
                };

                const slingBoxStatus = jsonData.slingBoxes[slingBoxName];
                if (slingBoxStatus) {
                    const properties = Object.entries(slingBoxStatus);
                    console.log(`Properties of "${slingBoxName}":`);
                    properties.forEach(([key, value]) => {
                        console.log(`${key}:`, value);
                        serverStatus[key] = value;
                    });
                } else {
                    console.log(`Item "${slingBoxName}" not found.`);
                }

                return serverStatus;
            })
            .catch(error => {
                console.error(`Error fetching file: ${jsonFileName}`, error);
                return null;
            });
    }
}



// Set the form's action to the current path
function setFormAction() {
    const form = document.getElementById("mainForm");
    form.action = window.location.pathname;
}


function setTitle() {
    let slingBoxName = getSlingBoxName();
    slingBoxName = toTitleCase(slingBoxName) + " TV";

    const titleElem = document.getElementById("titleElem");
    if (!isNullOrUndefinedOrEmpty(titleElem)) {
        titleElem.innerHTML = slingBoxName;

        const fontColor = getColorByFirstLetter(slingBoxName);
        titleElem.style.color = fontColor;
    }
}

function toTitleCase(str) {
    return str.replace(/\w\S*/g, function (txt) {
        return txt.charAt(0).toUpperCase() + txt.substr(1).toLowerCase();
    });
}

function getSlingBoxName() {
    const slingBoxName = window.location.pathname.split("/").pop();
    return slingBoxName;
}


// Create the query string params to be sent to the TvGuide webpage, so that it can call back this server
function setTvGuideButtonParameters() {
    const slingBoxName = getSlingBoxName();
    const slingStatusPath = "../SlingBoxStatus.json";

    DataAccess.readSlingBoxStatus(`${slingBoxName}`, slingStatusPath)
        .then(slingBoxStatus => {
            if (slingBoxStatus) {
                console.log("SlingBoxStatus:", slingBoxStatus);

                setTvGuideButtonQueryString(slingBoxStatus);
                setSlingIsAnalogue(slingBoxStatus);
            }
        });
}


function setTvGuideButtonQueryString(slingBoxStatus) {

    const tvGuideButtonElem = document.getElementById("btnTvGuide");
    if (isNullOrUndefinedOrEmpty(tvGuideButtonElem)) {
        return;
    }

    const slingServerUrl = window.location.protocol + "//" + window.location.hostname + ":" + window.location.port;
    const encodedSlingServerUrl = encodeURIComponent(slingServerUrl);

    const tvGuideUrl = slingBoxStatus.tvGuideUrl;

    const slingRemoteControlUrl = slingBoxStatus.slingRemoteControlUrl;
    const encodedSlingRemoteControlUrl = encodeURIComponent(slingRemoteControlUrl);


    displayDebugInfo(slingBoxStatus);


    let hrefValue = tvGuideUrl + "?slingServerUrl=" + encodedSlingServerUrl + "&" + "slingRemoteControlUrl=" + encodedSlingRemoteControlUrl;
    hrefValue = appendTimeStampToUrl(hrefValue);

    tvGuideButtonElem.href = hrefValue;
}


function displayDebugInfo(slingBoxStatus) {

    const displayDoc = document.getElementById("displayDebugInfoHere");

    if (isNullOrUndefinedOrEmpty(displayDoc)) {
        return;
    }

    const currentChannelNumber = slingBoxStatus.currentChannelNumber;
    const lastChannelNumber = slingBoxStatus.lastChannelNumber;
    const isAnalogue = slingBoxStatus.isAnalogue;
    const urlBase = slingBoxStatus.urlBase;
    const tvGuideUrl = slingBoxStatus.tvGuideUrl;
    const slingRemoteControlUrl = slingBoxStatus.slingRemoteControlUrl;
    const slingBoxName = getSlingBoxName();

    displayDoc.innerHTML +=
        "channel: " + currentChannelNumber + "  " +
        "last: " + lastChannelNumber + "   " +
        "isAnalogue: " + isAnalogue + "   " +
        "urlBase: " + urlBase + "<br>" +
        "tvGuideUrl: " + tvGuideUrl + "<br/>" +
        "slingRemoteControlUrl: " + slingRemoteControlUrl + "<br/>" +
        "slingBoxName: " + slingBoxName;
    displayDoc.style.textAlign = "left";
}


function setSlingIsAnalogue(config) {
    window.isAnalogue = config.isAnalogue;
    console.log("isAnalogue:", window.isAnalogue);
}


function getColorByFirstLetter(word) {
    const colors = ["red", "darkmagenta", "blue", "green", "cyan"];
    const firstLetter = word.charAt(0).toUpperCase();
    const asciiValue = firstLetter.charCodeAt(0);

    const colorIndex = asciiValue % colors.length;

    return colors[colorIndex];
}


// Set the form's action to the current path
function setFormAction() {
    const form = document.getElementById("mainForm");
    form.action = window.location.pathname;
}

// Attach event listeners to all buttons to highlight them when clicked
function setButtonHighlighting() {
    const buttons = document.querySelectorAll(".button, .round, .emptyButton");
    buttons.forEach(button => {
        button.addEventListener("click",
            () => {
                changeButtonColor(button);
            });
    });
}


// Attach event listener to the text input for Enter key to highlight it
function setTextBoxHighlighting() {
    const digitsInput = document.querySelector("input[name='Digits']");
    digitsInput.addEventListener("keydown", event => {
        if (event.key === "Enter") {
            highlightTextInput(digitsInput);
        }
    });
}



function changeButtonColor(button) {
    // Prevent highlighting buttons if the focus is on the text input
    if (document.activeElement.name === "Digits") {
        return;
    }

    // Change the button color temporarily
    const originalColor = button.style.backgroundColor;
    const originalTextColor = button.style.color;

    button.style.backgroundColor = "greenyellow"; //#B4B4B4";//yellow";
    button.style.color = "black";

    // Reset the color after 1000 milliseconds (1 second)
    setTimeout(() => {
        button.style.backgroundColor = originalColor;
        button.style.color = originalTextColor;
    }, 1000);
}


function highlightTextInput(input) {
    // Highlight the text input temporarily
    const originalBackgroundColor = input.style.backgroundColor;

    input.style.backgroundColor = "yellow";

    setTimeout(() => {
        input.style.backgroundColor = originalBackgroundColor;
    }, 1500);
}


function addAjaxButtonClickEventListener() {
    const ajaxButtons = document.querySelectorAll(".ajax-button");
    ajaxButtons.forEach(function (button) {
        button.addEventListener("click", function (event) {
            event.preventDefault();
            const buttonValue = button.value;

            sendButtonAjaxRequest(buttonValue);
        });
    });
}

function addAjaxChannelButtonClickEventListener() {
    // Attach click event to the button submitting the text box
    const submitTextButton = document.querySelector(".submit-text-button");
    submitTextButton.addEventListener("click",
        function (event) {
            event.preventDefault();

            // Special behavior for the button submitting the text box
            const textBoxValue = document.querySelector(".text").value;

            if (isNullOrUndefinedOrEmpty(textBoxValue)) {
                return;
            };

            const parsedChannelNumber = parseChannelNumber(textBoxValue);

            sendTextBoxAjaxRequest(parsedChannelNumber);

            updateChannelPlaceholder(parsedChannelNumber);
        });

}


function parseChannelNumber(channelNumber) {

    if (isNullOrUndefinedOrEmpty(channelNumber)) {
        return "";
    }

    // Remove non-digit and non-dot characters
    channelNumber = channelNumber.replace(/[^\d.]/g, "");

    if (!SlingBoxStatus.isAnalogue) {
        // Ensure the digital channel number is 4 digits by prepending '0' if needed
        if (channelNumber.length < 4) {
            channelNumber = "0".repeat(4 - channelNumber.length) + channelNumber;
        }
    }
    else {

        if (!channelNumber.includes(".")) {
            channelNumber += ".";
        } else {
            // Remove extra dots from analog channel number
            channelNumber = channelNumber.replace(/\.(?=.*\.)/g, "");
        }
    }

    return channelNumber;
}

function addAjaxTextBoxKeydownEventListener() {
    const textBox = document.querySelector(".text");
    textBox.addEventListener(
        "keydown",
        function (event) {
            // Check if the pressed key is Enter (key code 13)
            if (event.keyCode !== 13)
                return;

            event.preventDefault(); // Prevent the default form submission

            const parsedChannelNumber = parseChannelNumber(textBox.value);

            if (isNullOrUndefinedOrEmpty(parsedChannelNumber)) {
                return;
            };

            sendTextBoxAjaxRequest(parsedChannelNumber);

            updateChannelPlaceholder(parsedChannelNumber);
        });
}


function updateChannelPlaceholder(parsedChannelNumber) {
    const textBox = document.querySelector(".text");
    textBox.placeholder = parsedChannelNumber;
    textBox.value = "";

    // Set a timeout to clear the placeholder
    setTimeout(() => {
        textBox.placeholder = "";
    }, 15000);

    // Blur the text input to hide the keyboard on mobile devices
    textBox.blur();
}


function sendButtonAjaxRequest(buttonValue) {
    const formData = new FormData();
    formData.append("buttonValue", buttonValue);

    fetch(window.location.pathname, {
        method: "POST",
        body: formData
    })
        .then(response => response.text())
        .then(() => {
            // Clear the placeholder
            const textBox = document.querySelector(".text");
            textBox.placeholder = "";
        })
        .catch(error => {
            console.error("Error:", error);
        });
}


function sendTextBoxAjaxRequest(digitsValue) {
    const requestData = new URLSearchParams();
    requestData.append("Channel", "Channel");
    requestData.append("Digits", digitsValue);

    fetch(window.location.pathname, {
        method: "POST",
        body: requestData.toString(),
        headers: {
            'Content-Type': "application/x-www-form-urlencoded"
        }
    })
        .then(response => response.text())
        //.then(data => {
        //    console.log('Server response length:', data.length);
        //})
        .catch(error => {
            console.error("Error:", error);
        });
}