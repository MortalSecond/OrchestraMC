# OrchestraMC

*A lightweight Minecraft server manager that automates Forge setup, tunneling, and hosting.*

OrchestraMC is a small, lightweight tool i made for my friends to simplify the server-hosting process. It is designed to take the pain out of running Minecraft modded servers. Instead of juggling Forge installers, tunneling services like Playit.gg or Ngrok, and manual server.properties edits, OrchestraMC handles everything from a single clean UI.

---

## 1. For Users

### 1.1. Preamble

Before using OrchestraMC, make sure you already have one of the supported tunneling/hosting methods ready:

* **Playit.gg** (recommended for most users — easy and free).
* **Ngrok** (requires an Ngrok account and setup).
* **Port Forwarding** (requires router access and manual setup on port `25565`).

Each option has pros and cons, but if you’re just starting out, Playit.gg is the most beginner-friendly.

---

### 1.2. Installation

1. Download the latest release `.rar` file from [Releases](https://github.com/MortalSecond/OrchestraMC/releases).
2. Extract it anywhere on your computer (no special directory needed).
3. Launch **MinecraftServerTool.exe** to start OrchestraMC.

That’s it. No installers, no extra configuration. OrchestraMC runs standalone.

---

### 1.3. Usage

When you first launch OrchestraMC, you’ll see the main window six different forms you can interact with..

#### 1.3.1. Modpack Folder

This is where you locate the Minecraft instance you want Orchestra to use. Orchestra directly uses the location of the modpack, to simplify the process.

* If you’re using **CurseForge**, your instances are usually stored at:

  ```
  C:\Users\[YourUser]\curseforge\minecraft\Instances\[YourModpack]
  ```
  
---

#### 1.3.2. Minecraft Version

Here you’ll select the Minecraft build your modpack is using. This ensures the correct Forge and server settings are applied.

---

#### 1.3.3. Forge Version

This lets you select from a few types of available Forge builds. For simplicity:

* **Latest Stable** → The recommended Forge build from Maven.
* **Latest Experimental** → The latest, most up-to-date Forge build Forge build available (may be unstable).
* **Custom Version** → If your modpack has Forge incompatibilities, or for any reason needs a specific Forge version, you can use the custom version option, which will give you a dropdown menu to choose a specific version from.

---

#### 1.3.4. Hosting Method

Here you choose which tunneling service you want to use. It is HEAVILY recommended to use Playit.gg unless you know what you're doing; each option carries its pros and cons, but otherwise Playit is the most beginner-friendly.

* **Playit.gg** → Quick, reliable tunneling, best for new users.
* **Ngrok** → Flexible but requires an account.
* **Port Forwarding** → Manual setup through your router.

---

#### 1.3.5. Server Address

Once the server is running, this field will update with the public address your friends can use to connect to the server with. You can click it to quickly copy it to your clipboard.

---

#### 1.3.6. Run Server

This big central button is the heart of OrchestraMC:

* If Forge is not yet installed → Installs Forge for your modpack.
* If tunnel service is missing → Installs or launches Playit/Ngrok.
* If everything is ready → Starts your server and tunnel service in one go.
* If already running → Stops both the server and tunnel service.

For finer control, use the dropdown menu next to the Run button to:

* Install Forge
* Start Server
* Restart Server
