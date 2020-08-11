function saveFile(filename, content) {
    const a = document.createElement("a");
    const file = new Blob([content], { type: "text/plain" });
    a.href = URL.createObjectURL(file);
    a.download = filename.replace(/[^a-zA-Z0-9\.]/g, '_');
    a.click();
}

async function readFile(name) {
   
    const input = document.getElementById(name);
    const files = input.files;
    var file = files[0];
    var reader = new FileReader();

    return await new Promise((resolve, reject) => {
        reader.onerror = () => {
            reader.abort();
            reject(new DOMException("Problem parsing input file."));
        };

        reader.onload = () => resolve(reader.result);

        reader.readAsText(file);
    });
}

window.saveFile = saveFile;
window.readFile = readFile;
