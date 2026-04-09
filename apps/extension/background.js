chrome.runtime.onInstalled.addListener(() => {
  chrome.sidePanel.setPanelBehavior({ openPanelOnActionClick: true });
});

chrome.action.onClicked.addListener((tab) => {
  if (!tab?.id) {
    return;
  }

  chrome.sidePanel.open({ tabId: tab.id });
});
