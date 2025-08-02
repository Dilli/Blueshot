# Blueshot Toolbar Icons - Canva Design Guide

## 🎨 Design Specifications

### **Icon Dimensions**
- **Size**: 32x32 pixels (upgraded from 24x24 for better visibility)
- **Format**: PNG with transparency
- **Style**: Modern, flat design with subtle shadows
- **Color Scheme**: VS Code blue (#007ACC), white accents, consistent with application theme

### **Canva Design Setup**
1. Go to: https://www.canva.com/create/icons/
2. Select "Custom size" → 32x32 pixels (upgraded from 24x24)
3. Choose transparent background
4. Use VS Code blue theme consistently across all icons

## 📋 Required Icons List

### **Main Toolbar Icons**
1. **save.png** - Save/Floppy disk icon
2. **copy.png** - Copy/Clipboard icon
3. **zoom-in.png** - Magnifying glass with "+"
4. **zoom-out.png** - Magnifying glass with "-"
5. **actual-size.png** - 1:1 ratio or "100%" text
6. **fit-window.png** - Expand/fit to window icon

### **Annotation Tools**
7. **select.png** - Arrow cursor/pointer
8. **highlight.png** - Highlighter pen
9. **rectangle.png** - Square/rectangle outline
10. **line.png** - Straight line
11. **arrow.png** - Arrow pointing right
12. **text.png** - "T" text icon
13. **counter.png** - Circle with "1" inside
14. **crop.png** - Crop/scissors icon

## 🎯 Design Guidelines for Each Icon

### **Save Icon** (save.png)
- Traditional floppy disk or modern download arrow
- Color: VS Code blue (#007ACC) with white accents
- Style: Outlined with 2px stroke

### **Copy Icon** (copy.png)
- Two overlapping rectangles or clipboard
- Color: VS Code blue with white accents
- Subtle shadow to show layering

### **Zoom Icons** (zoom-in.png, zoom-out.png)
- Magnifying glass with + or - symbols
- Color: VS Code blue handle and lens
- Clear, bold + and - symbols in white

### **Size Icons** (actual-size.png, fit-window.png)
- actual-size: "1:1" text or percentage symbol
- fit-window: Expand arrows in corners
- Color: VS Code blue primary

### **Select Tool** (select.png)
- Arrow cursor (pointing up-left)
- Color: VS Code blue with white outline
- Clean, precise shape

### **Highlight Tool** (highlight.png)
- Highlighter marker shape
- Color: VS Code blue body with white accents
- Slight angle for dynamic look

### **Rectangle Tool** (rectangle.png)
- Simple square outline
- Color: VS Code blue stroke, transparent fill
- 2px stroke width

### **Line Tool** (line.png)
- Diagonal line from corner to corner
- Color: VS Code blue
- 2px thickness

### **Arrow Tool** (arrow.png)
- Right-pointing arrow
- Color: VS Code blue
- Bold, clear arrowhead

### **Text Tool** (text.png)
- Letter "T" or "Aa"
- Color: VS Code blue
- Bold, sans-serif font

### **Counter Tool** (counter.png)
- Circle with number "1" inside
- Color: VS Code blue circle, white number
- Bold, centered number

### **Crop Tool** (crop.png)
- Crop corners (L-shapes in corners)
- Color: VS Code blue
- Or scissors icon alternative

## 📁 Export Settings

### **From Canva:**
1. Download as PNG
2. Ensure transparent background
3. Name files exactly as specified above
4. Create both 24x24 and 48x48 versions

### **File Naming Convention:**
- 32x32: `iconname.png`
- 64x64: `iconname@2x.png` (for high DPI displays)

## 🔄 Integration Steps

After designing in Canva:
1. Download all icons to the `icons/` folder
2. Update the C# code to load from files instead of generating programmatically
3. Test the new icons in the application
4. Adjust colors/size if needed

## 🎨 Color Palette

```
VS Code Blue: #007ACC (Primary color for all UI elements)
White: #FFFFFF (Text and accents)
Disabled Blue: #00508E (Muted blue for disabled states)
Muted White: #C8C8C8 (Text on disabled elements)
```

## ✅ Quality Checklist

- [ ] All 14 icons designed
- [ ] Consistent VS Code blue theme throughout
- [ ] 32x32 and 64x64 versions
- [ ] Transparent backgrounds
- [ ] PNG format
- [ ] Clear visibility at small sizes
- [ ] Professional VS Code-inspired appearance
