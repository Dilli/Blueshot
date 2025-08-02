# Blueshot Toolbar Icons - Canva Design Guide

## 🎨 Design Specifications

### **Icon Dimensions**
- **Size**: 24x24 pixels (for high DPI: also create 48x48)
- **Format**: PNG with transparency
- **Style**: Modern, flat design with subtle shadows
- **Color Scheme**: Primary blue (#2563EB), dark gray (#374151), white accents

### **Canva Design Setup**
1. Go to: https://www.canva.com/create/icons/
2. Select "Custom size" → 24x24 pixels
3. Choose transparent background
4. Use consistent style across all icons

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
- Color: Dark gray (#374151) with blue accent
- Style: Outlined with 2px stroke

### **Copy Icon** (copy.png)
- Two overlapping rectangles or clipboard
- Color: Dark gray with blue accent
- Subtle shadow to show layering

### **Zoom Icons** (zoom-in.png, zoom-out.png)
- Magnifying glass with + or - symbols
- Color: Blue handle, dark gray lens
- Clear, bold + and - symbols

### **Size Icons** (actual-size.png, fit-window.png)
- actual-size: "1:1" text or percentage symbol
- fit-window: Expand arrows in corners
- Color: Blue primary, gray secondary

### **Select Tool** (select.png)
- Arrow cursor (pointing up-left)
- Color: Dark gray with white outline
- Clean, precise shape

### **Highlight Tool** (highlight.png)
- Highlighter marker shape
- Color: Yellow/orange tip, blue body
- Slight angle for dynamic look

### **Rectangle Tool** (rectangle.png)
- Simple square outline
- Color: Blue stroke, transparent fill
- 2px stroke width

### **Line Tool** (line.png)
- Diagonal line from corner to corner
- Color: Blue
- 2px thickness

### **Arrow Tool** (arrow.png)
- Right-pointing arrow
- Color: Blue
- Bold, clear arrowhead

### **Text Tool** (text.png)
- Letter "T" or "Aa"
- Color: Blue
- Bold, sans-serif font

### **Counter Tool** (counter.png)
- Circle with number "1" inside
- Color: Blue circle, white number
- Bold, centered number

### **Crop Tool** (crop.png)
- Crop corners (L-shapes in corners)
- Color: Blue
- Or scissors icon alternative

## 📁 Export Settings

### **From Canva:**
1. Download as PNG
2. Ensure transparent background
3. Name files exactly as specified above
4. Create both 24x24 and 48x48 versions

### **File Naming Convention:**
- 24x24: `iconname.png`
- 48x48: `iconname@2x.png`

## 🔄 Integration Steps

After designing in Canva:
1. Download all icons to the `icons/` folder
2. Update the C# code to load from files instead of generating programmatically
3. Test the new icons in the application
4. Adjust colors/size if needed

## 🎨 Color Palette

```
Primary Blue: #2563EB
Dark Gray: #374151
Light Gray: #9CA3AF
White: #FFFFFF
Accent Orange: #F59E0B (for highlights)
```

## ✅ Quality Checklist

- [ ] All 14 icons designed
- [ ] Consistent style and color scheme
- [ ] 24x24 and 48x48 versions
- [ ] Transparent backgrounds
- [ ] PNG format
- [ ] Clear visibility at small sizes
- [ ] Professional appearance
