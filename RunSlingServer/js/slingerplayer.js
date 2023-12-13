/*********************************************
 * Ensure the channel number text contains a dot after it, given that this is an analogue channel
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

    // Load JSON text from server hosted file and return JSON parsed object
    static getJsonString(filePath) {
        const jsonString = DataAccess.loadDataFromWebApiSync(filePath, "application/json");

        return JSON.parse(jsonString);
    }

    // Load text with Ajax synchronously: takes path to file and optional MIME type
    static loadDataFromWebApiSync(filePath, mimeType) {
        const xmlhttp = new XMLHttpRequest();
        xmlhttp.withCredentials = false; //indicates whether or not cross-site Access-Control requests should be made using credentials such as cookies, authorization headers or TLS client certificate
        //xmlhttp.timeout = 2000; // 2 seconds: only for async
        const isAsync = false;

        xmlhttp.open("GET", filePath, isAsync);

        if (mimeType != null) {
            if (xmlhttp.overrideMimeType) {
                xmlhttp.overrideMimeType(mimeType);
            }
        }

        xmlhttp.send();

        if (xmlhttp.status === 200) {
            return xmlhttp.responseText;
        } else {
            console.log("AJAX error =" + xmlhttp.statusText);

            return null;
        }
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
