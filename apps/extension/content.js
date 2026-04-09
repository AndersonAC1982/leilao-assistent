function sanitizeText(value) {
  return String(value || "")
    .replace(/\s+/g, " ")
    .trim();
}

function getVehicleHint() {
  const title = sanitizeText(document.title);
  if (!title) {
    return "";
  }

  const separators = ["|", "-"];
  for (const separator of separators) {
    const pieces = title
      .split(separator)
      .map((piece) => sanitizeText(piece))
      .filter(Boolean);

    if (pieces.length > 0 && pieces[0].length >= 6) {
      return pieces[0];
    }
  }

  return title;
}

chrome.runtime.onMessage.addListener((request, _sender, sendResponse) => {
  if (request?.type !== "LEILAOAUTO_TAB_CONTEXT") {
    return;
  }

  sendResponse({
    href: window.location.href,
    domain: window.location.hostname,
    title: sanitizeText(document.title),
    vehicleHint: getVehicleHint()
  });
});
