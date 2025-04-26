# 📥 DOWNLOAD APP

- [ANDROID Version](https://github.com/IgrisModz/EuroGen/releases/download/1.1.1.0/com.companyname.eurogen.apk)
- [WINDOWS Version](https://github.com/IgrisModz/EuroGen/releases/download/1.1.1.0/EuroGen_1.1.1.0-Windows.zip)

## Installing EuroGen - ONLY FOR WINDOWS (.msix with self-signed certificate)

Welcome to **EuroGen**!

This guide will explain how to install the application from the `.msix` file, and how to trust the self-signed certificate to avoid the "**Unknown Publisher**" warning.

---

## 📦 Provided Files

- `EuroGen_1.1.1.0_x64.msix` → The application installer
- `EuroGen_1.1.1.0_x64.cer` → The self-signed certificate used to sign the installer

---

## 🛠️ Installation Steps

### 1. Install the self-signed certificate

Before installing the application, you need to trust the certificate to prevent security warnings.

- Double-click the **`EuroGen_1.1.1.0_x64.cer`** file.
- Click on **Install Certificate**.
- Choose **Local Machine** (you might need administrator rights).
- Select **Place all certificates in the following store**.
- Choose **Trusted Root Certification Authorities**.
- Complete the wizard to finish the installation.

✅ **Your computer now trusts the certificate.**

---

### 2. Install the .msix application

- Double-click the **`EuroGen_1.1.1.0_x64.msix`** file.
- A Windows installation window will appear.
- Click **Install**.

✅ **The application will install without the "Unknown Publisher" warning.**

---

## 🚑 Troubleshooting

- If you still see a warning:
  - Make sure the certificate is installed under **Trusted Root Certification Authorities** for the **Local Machine**.
  - Restart your computer after installing the certificate (rarely necessary).
- If Windows blocks the installation, check if your antivirus or SmartScreen is preventing the `.msix` file from running.

---

## 🧹 Uninstalling the Application

- Open **Settings** > **Apps** > **Installed Apps**.
- Find **EuroGen**.
- Click **Uninstall**.

---

## 📚 Useful Resources

- [Installing .msix apps on Windows](https://learn.microsoft.com/en-us/windows/msix/overview)
- [Install a root certificate on Windows](https://learn.microsoft.com/en-us/windows-server/security/windows-authentication/supported-authentication-protocols)

---

## 📃 License

Project licensed under MIT License. See [LICENSE](LICENSE) for more details.

---

## ✨ Thank you for using EuroGen!
