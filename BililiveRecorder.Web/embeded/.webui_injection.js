(function () {
    const currentScript = document.currentScript;
    if (currentScript && "string" === typeof currentScript.dataset.href) {
        const SERVER_PATH = currentScript.dataset.href;
        const baseTag = document.getElementsByTagName('base')[0];
        console.log("SERVER_PATH: " + SERVER_PATH);
        const pathname = location.pathname;
        console.log("location.pathname: " + pathname);
        if (SERVER_PATH.length === 0) {
            let BASE = pathname + '/';
            baseTag.href = BASE;
            console.log("base path: " + BASE);
        } else if (pathname.endsWith(SERVER_PATH)) {
            var i = pathname.lastIndexOf(SERVER_PATH);
            if (i > -1) {
                let BASE = pathname.slice(0, i) + '/';
                baseTag.href = BASE;
                console.log("base path: " + BASE);
            }
        } else {
            console.log('????');
        }
    }
    const init = document.getElementById('delayed-init');
    document.head.append(init.content.cloneNode(true));
    init.remove();
    currentScript.remove();
})()
