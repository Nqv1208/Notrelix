// The script to inject into <head> to prevent color theme flash
export const colorThemeScript = `
(function() {
  try {
    var theme = localStorage.getItem('notrelix-color-theme');
    if (theme && theme !== 'default') {
      document.documentElement.classList.add('theme-' + theme);
    }
  } catch (e) {}
})()
`;
