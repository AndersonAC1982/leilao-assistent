chrome.runtime.onMessage.addListener((request, _sender, sendResponse) => {
  if (request?.type !== 'LEILAOAUTO_TAB_CONTEXT') {
    return;
  }

  sendResponse({
    href: window.location.href,
    domain: window.location.hostname,
    title: document.title
  });
});
