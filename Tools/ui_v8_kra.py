"""Assemble the SHHH! V8 painted masters into layered Krita documents (.kra), run with:
   QT_QPA_PLATFORM=offscreen kritarunner -s ui_v8_kra -f main   (from Tools/, with PYTHONPATH including Tools/)
Layers come from DesignSources/UI_V8/src (procedural paintings) so artists can repaint any layer in Krita and re-export."""
import os
from krita import Krita
ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), ".."))
SRC = os.path.join(ROOT, "DesignSources", "UI_V8", "src"); OUT = os.path.join(ROOT, "DesignSources", "UI_V8", "kra")

def build(name, w, h, layers):
    app = Krita.instance()
    doc = app.createDocument(w, h, name, "RGBA", "U8", "", 72.0)
    root = doc.rootNode()
    for lname, path in layers:
        node = doc.createNode(lname, "paintlayer")
        root.addChildNode(node, None)
        from PyQt5.QtGui import QImage
        img = QImage(path).convertToFormat(QImage.Format_RGBA8888)
        img = img.rgbSwapped()  # Krita RGBA U8 pixel data is BGRA
        ptr = img.constBits(); ptr.setsize(img.byteCount())
        node.setPixelData(bytes(ptr), 0, 0, img.width(), img.height())
    # remove the default background layer if present
    for c in root.childNodes():
        if c.name() in ("Background", "Paint Layer 1") and c.name() not in [l[0] for l in layers]: c.remove()
    doc.refreshProjection()
    os.makedirs(OUT, exist_ok=True)
    doc.saveAs(os.path.join(OUT, name + ".kra"))   # validation: the .kra's mergedimage.png (Krita's own flattening)
    doc.close()

def main(*args):
    build("SHHH_NightSky_Panorama", 4096, 2048, [(n, os.path.join(SRC, f"sky_{n}.png")) for n in ("gradient", "clouds", "stars", "moon", "town")])
    print("kra written to", OUT)
