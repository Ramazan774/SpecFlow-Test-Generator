# StepForge - Cross-Browser Extension

## 📋 Files Overview

- **manifest-chrome.json** - Chrome/Edge manifest (uses `service_worker`)
- **manifest-firefox.json** - Firefox manifest (uses `scripts` array)
- **manifest.json** - Currently active manifest (defaults to Chrome version)
- **build.sh** - Build script to create browser-specific packages

## 🧪 Testing Locally

### Chrome (Current Setup)
1. Go to `chrome://extensions/`
2. Enable "Developer mode"
3. Click "Load unpacked"
4. Select this directory
5. ✅ Chrome is using `manifest.json` (Chrome version)

### Firefox
```bash
# Use the Firefox-specific manifest
cp manifest-firefox.json manifest.json

# Run web-ext
web-ext run
```

**Or** to keep Chrome working while testing Firefox:
```bash
# Test Firefox without changing manifest.json
cd ..
mkdir StepForgeFirefox
cp -r StepForgeExtension/* StepForgeFirefox/
cd StepForgeFirefox
cp manifest-firefox.json manifest.json
web-ext run
```

## 🚀 Building for Distribution

Run the build script:

```bash
./build.sh
```

This creates:
- `build/stepforge-chrome-v1.0.0.zip` - Submit to Chrome Web Store
- `build/stepforge-firefox-v1.0.0.zip` - Submit to Firefox Add-ons

## 📝 Publishing Checklist

### Chrome Web Store
- [ ] Use `build/stepforge-chrome-v1.0.0.zip`
- [ ] Verify manifest uses `service_worker`
- [ ] Test in Chrome before uploading
- [ ] Pay $5 registration fee (one-time)

### Firefox Add-ons
- [ ] Use `build/stepforge-firefox-v1.0.0.zip`
- [ ] Verify manifest uses `scripts` array
- [ ] Test with web-ext before uploading
- [ ] No registration fee (FREE)

## 🔄 Development Workflow

**When developing:**

1. **For Chrome testing**: 
   - Keep `manifest.json` as-is (Chrome version)
   - Reload extension in `chrome://extensions/`

2. **For Firefox testing**:
   ```bash
   cp manifest-firefox.json manifest.json
   web-ext run
   # Press Ctrl+C when done
   cp manifest-chrome.json manifest.json  # Restore Chrome version
   ```

3. **Before committing changes**:
   - Ensure `manifest.json` = `manifest-chrome.json`
   - Both code files (background.js, content.js) work in both browsers
   - Run `./build.sh` to verify packages build correctly

## 🎯 Key Differences

| Feature | Chrome | Firefox |
|---------|--------|---------|
| **Background** | `service_worker` | `scripts` array |
| **Browser API** | `chrome.*` | `browser.*` (shimmed to `chrome`) |
| **Special Settings** | None | `browser_specific_settings.gecko` |
| **Downloads** | Blob URLs ✅ | Blob URLs ✅ |

## ⚙️ Compatibility Layer

Both files include this shim at the top:
```javascript
if (typeof browser !== 'undefined') globalThis.chrome = browser;
```

This allows the same code to work in both browsers!

## 📦 What Gets Built

The `build.sh` script:
1. Creates `build/chrome/` with Chrome manifest
2. Creates `build/firefox/` with Firefox manifest
3. Copies all `.js`, `.html`, `.css` files to both
4. Creates ZIP files ready for submission

## ✅ Ready to Publish

Your extension is now ready for **both** Chrome and Firefox stores! 🎉
