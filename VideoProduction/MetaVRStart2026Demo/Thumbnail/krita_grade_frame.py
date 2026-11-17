"""Krita stage of the thumbnail: load the real Quest frame (F09 5:31, two-hand scrub), crop to the action, lift saturation a touch,
sharpen (unsharp mask) and save both a layered .kra (original + graded layers, editable in Krita) and the graded PNG for Inkscape.
Run (from this folder):  set PYTHONPATH=. & kritarunner -s krita_grade_frame -f main"""
import os
from krita import Krita, InfoObject
from PyQt5.QtGui import QImage

HERE = os.path.dirname(os.path.abspath(__file__))
SRC = os.path.join(HERE, "frames", "f09_331.0.jpg")
X, Y, W, H = 660, 200, 1000, 750           # crop: both hands, sponge, stained plate, soap bottle (4:3)


def put(node, img):
    img = img.convertToFormat(QImage.Format_RGBA8888).rgbSwapped()
    ptr = img.constBits(); ptr.setsize(img.byteCount())
    node.setPixelData(bytes(ptr), 0, 0, img.width(), img.height())


def main(*args):
    app = Krita.instance()
    src = QImage(SRC).copy(X, Y, W, H)
    doc = app.createDocument(W, H, "SHHH_Thumbnail_Frame", "RGBA", "U8", "", 72.0)
    root = doc.rootNode()
    orig = doc.createNode("Quest frame F09 5:31 (original)", "paintlayer"); root.addChildNode(orig, None); put(orig, src)
    graded = doc.createNode("graded (HSV + unsharp)", "paintlayer"); root.addChildNode(graded, None); put(graded, src)
    for c in root.childNodes():
        if c.name() in ("Background", "Paint Layer 1"): c.remove()
    hsv = app.filter("hsvadjustment"); cfg = hsv.configuration()
    cfg.setProperty("h", 0); cfg.setProperty("s", 14); cfg.setProperty("v", 3); cfg.setProperty("type", 1); hsv.setConfiguration(cfg)
    hsv.apply(graded, 0, 0, W, H)
    us = app.filter("unsharp"); cfg = us.configuration()
    cfg.setProperty("halfSize", 2); cfg.setProperty("amount", 0.6); cfg.setProperty("threshold", 2); us.setConfiguration(cfg)
    us.apply(graded, 0, 0, W, H)
    doc.refreshProjection(); doc.waitForDone()
    doc.saveAs(os.path.join(HERE, "SHHH_Thumbnail_Frame.kra"))
    doc.setBatchmode(True)
    doc.exportImage(os.path.join(HERE, "frame_graded.png"), InfoObject())
    doc.close()
    print("krita: frame graded and saved")
