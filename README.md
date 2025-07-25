# Bantayan Emergency Mobile Application

**Capstone Project**  
**Bachelor of Science in Information Technology**

## Project Overview

The Bantayan Emergency Mobile Application is a mobile-based alert system developed to enhance emergency response coordination between residents and local authorities in Bantayan Island. This system enables residents to report emergencies in real-time and allows authorities to monitor, respond, and send out alerts more efficiently.

The application was developed using Xamarin.Forms for cross-platform compatibility, with Firebase serving as the backend for real-time data synchronization and push notifications.

## Features

### Resident Users
- Secure login and registration with valid ID verification
- Emergency reporting with photo and description
- Option to send emergency alerts to specific authorities (Police, Medics, BANELCO, Fire Department) or to all
- Directory of emergency contact numbers
- Real-time push notifications for warnings and announcements
- View the status of submitted emergency reports

### LGU/Authority Users
- Authorized login for LGU personnel
- Dashboard to view and filter submitted emergency reports
- Update report status (acknowledged, in-progress, resolved)
- Report abuse or false reports with justification
- Send push notifications to specific users or all residents

### Admin Panel
- Review and approve or reject user registrations
- Provide reasons for rejection
- Real-time dashboard showing counts of pending, approved, and rejected users
- Monitor all emergency reports submitted by residents
- Review and approve user profile changes before applying them
- View logs of all profile change requests and admin actions

## Technologies Used

- **Frontend:** Xamarin.Forms (C#)
- **Backend:** Firebase Realtime Database, Firebase Cloud Messaging
- **Authentication:** Firebase Authentication
- **Push Notifications:** Firebase Cloud Messaging
- **Image Storage:** Local device storage (for emergency images)

## Developers

- David Forrosuelo (Lead Developer)

## Acknowledgments

- Municipality of Bantayan for providing support and feedback during development
- Faculty of Salazar Colleges of Science and Institute of Technology – BSIT Program
- Xamarin and Firebase documentation and community forums

## License

This project was developed as a capstone requirement and is intended for academic and community use. All rights reserved by David P. Forrosuelo
