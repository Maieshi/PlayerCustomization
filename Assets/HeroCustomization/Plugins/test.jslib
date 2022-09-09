mergeInto(LibraryManager.library, {
  ExportTextureJS:function(base64){
    // 手順1
    const image = Pointer_stringify(base64);

    // 手順2
    var bin = atob(image.replace(/^.*,/, ''));
    var buffer = new Uint8Array(bin.length);
    for (var i = 0; i < bin.length; i++) {
      buffer[i] = bin.charCodeAt(i);
    }

    try{
      var blob = new Blob([buffer.buffer], {
        type: 'image/png'
      });
    }catch (e){
      return;
    }

    // 手順3
    var url = (window.URL || window.webkitURL);
    var dataUrl = url.createObjectURL(blob);

    var a = document.createElement('a');
    a.download = "picture.png";
    a.href = dataUrl;

    a.click();
  },
});