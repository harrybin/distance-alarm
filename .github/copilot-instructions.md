# Distance Alarm - .NET MAUI Project

## Project Overview

This is a .NET MAUI application designed for Android phones and Wear OS devices that implements a Bluetooth Low Energy (BLE) based distance alarm system.

## Key Features

- Cross-platform support (Android & Wear OS)
- Bluetooth Low Energy connectivity between devices
- Configurable ping intervals for connection monitoring
- Multiple alarm types (vibration, sound, notifications)
- Distance-based alarm triggers when BLE connection is lost

## Development Guidelines

- Use .NET MAUI framework for cross-platform development
- Implement BLE using Plugin.BLE or similar libraries
- Follow MVVM pattern for UI architecture
- Use dependency injection for platform-specific services
- Ensure proper permission handling for Bluetooth and notifications

## Project Structure

- `Platforms/` - Platform-specific implementations
- `Services/` - BLE and alarm services
- `ViewModels/` - MVVM view models
- `Views/` - UI pages and controls
- `Models/` - Data models and settings
