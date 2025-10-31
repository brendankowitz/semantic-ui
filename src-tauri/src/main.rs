// Prevents additional console window on Windows in release mode
#![cfg_attr(all(not(debug_assertions), target_os = "windows"), windows_subsystem = "windows")]

use std::process::{Child, Command};
use std::thread;
use std::time::Duration;
use tauri::{AppHandle, Manager};

static mut BACKEND_PROCESS: Option<Child> = None;

/// Spawn the .NET backend process
fn spawn_backend() -> Result<(), String> {
    let backend_exe = if cfg!(debug_assertions) {
        // Development: Use the already-built backend
        "..\\src\\SemanticUI.Api\\bin\\Debug\\net8.0\\SemanticUI.Api.exe"
    } else {
        // Production: Use the embedded backend
        "./backend/SemanticUI.Api.exe"
    };

    println!("Starting backend from: {}", backend_exe);

    let child = Command::new(backend_exe)
        .spawn()
        .map_err(|e| format!("Failed to spawn backend: {}", e))?;

    unsafe {
        BACKEND_PROCESS = Some(child);
    }

    // Wait for backend to be ready (ping /health endpoint)
    wait_for_backend_ready()?;

    Ok(())
}

/// Poll the backend health endpoint until it responds
fn wait_for_backend_ready() -> Result<(), String> {
    let max_attempts = 30; // 30 seconds with 1-second intervals
    let mut attempt = 0;

    loop {
        attempt += 1;

        // Try to connect to the health endpoint
        match std::net::TcpStream::connect("127.0.0.1:5050") {
            Ok(_) => {
                println!("Backend is ready!");
                return Ok(());
            }
            Err(_) => {
                if attempt >= max_attempts {
                    return Err(format!(
                        "Backend failed to start after {} attempts",
                        max_attempts
                    ));
                }
                thread::sleep(Duration::from_millis(1000));
            }
        }
    }
}

/// Kill the backend process gracefully
fn kill_backend() {
    unsafe {
        if let Some(mut child) = BACKEND_PROCESS.take() {
            println!("Stopping backend...");
            let _ = child.kill();
            let _ = child.wait();
            println!("Backend stopped");
        }
    }
}

fn main() {
    tauri::Builder::default()
        .setup(|app| {
            // Spawn the backend on app startup
            if let Err(e) = spawn_backend() {
                eprintln!("Failed to start backend: {}", e);
                std::process::exit(1);
            }

            // Log the app handle for debugging
            println!("Tauri app initialized successfully");

            Ok(())
        })
        .on_window_event(|event| {
            match event.event() {
                tauri::WindowEvent::CloseRequested { api, .. } => {
                    // Handle window close - don't exit app
                    api.prevent_close();
                }
                _ => {}
            }
        })
        .invoke_handler(tauri::generate_handler![])
        .build(tauri::generate_context!())
        .expect("error while running tauri application")
        .run(|app_handle, event| {
            match event {
                tauri::RunEvent::ExitRequested { api, .. } => {
                    // Kill backend when app exits
                    kill_backend();
                    api.exit();
                }
                _ => {}
            }
        });
}
