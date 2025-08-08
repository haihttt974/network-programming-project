# 🎯 DKyThucTap

DKyThucTap is an internship management platform built with ASP.NET Core 9. The system allows candidates to search and apply for internships, while recruiters manage postings and track applications in real time. The project demonstrates modern web development techniques including SignalR real-time communication, layered architecture, and role-based security.

## ✨ Features
- **Authentication & Authorization**
  - Account registration and login using hashed passwords.
  - Role-based access control (candidate, recruiter, admin).
- **Profile Management**
  - Rich user profiles with personal details, skills, and CV uploads.
- **Company & Job Position Management**
  - Create and manage companies, invite recruiters, and post internship positions.
  - Track position statistics and status history.
- **Application Tracking**
  - Candidate job applications with cover letters and additional information.
  - Status updates with history and interviewer notes.
- **Real-Time Notifications**
  - SignalR powered toast notifications and badge counter.
  - Fallback polling and multi-tab synchronization.
- **Real-Time Chat**
  - Private messaging between users using SignalR chat hub.
- **Online User Tracking**
  - Service for tracking connections and active users.
- **Admin Dashboard**
  - Admin area for managing users, companies, positions, and applications.
- **Extensible Architecture**
  - Services, DTOs, and clear separation of concerns for easy expansion.

## 🗂️ Project Structure
```
DKyThucTap/\
├── Areas/\
│   └── Admin/                     # Admin area controllers and views\
├── Controllers/                   # MVC controllers\
├── Data/                          # EF Core DbContext and SQL script\
├── Hubs/                          # SignalR hubs (ChatHub, NotificationHub)\
├── Models/\
│   └── DTOs/                      # Data transfer objects\
├── Services/                      # Business and integration services\
├── ViewModels/                    # View models for Razor pages\
├── Views/                         # Razor views\
├── wwwroot/                       # Static files (css, js, images)\
├── doc_noti.md                    # Detailed notification system documentation\
└── DKyThucTap.csproj              # .NET project file
````

## ✅ Requirements
- [.NET SDK 9.0](https://dotnet.microsoft.com/)
- SQL Server 2019 or later
- [EF Core Tools](https://learn.microsoft.com/ef/core/cli/dotnet) for migrations
- Node.js & npm (optional, for managing front-end assets)
- An IDE such as Visual Studio 2022 or VS Code

## 🚀 Getting Started
1. **Clone the repository**
   ```bash
   git clone <repo-url>
   cd network-programming-project/DKyThucTap
   ```
2. **Configure database**
   - Create a SQL Server database.
   - Update the connection string in `appsettings.json`:
     ```json
     {
       "ConnectionStrings": {
         "DKyThucTapContext": "Server=.;Database=DKyThucTap;Trusted_Connection=True;TrustServerCertificate=True"
       }
     }
     ```
   - Import the schema using `full-sql-code.sql` or run EF Core migrations:
     ```bash
     dotnet ef database update
     ```
3. **Run the application**
   ```bash
   dotnet run
   ```
   The site will start on `http://localhost:5000` or `https://localhost:5001`.
4. **Explore**
   - Register a new account and log in.
   - Use multiple browser tabs to test real-time notifications and chat.
   - Visit `/Test/Notification` for notification debugging utilities.

## 📌 Notes
- Replace placeholder connection strings and credentials before deploying.
- The `doc_noti.md` file contains exhaustive documentation for the notification system.
- Static files are served from `wwwroot` and can be customized without recompiling.
- Ensure HTTPS is configured in production environments.
- Some features assume seeded data (roles, demo users, etc.); adjust as needed.

## 🔒 License
Distributed under the MIT License. See `LICENSE` for details.

## The Dev Team
<div align="center">
	<table>
		<tr>
			<td align="center" valign="top">
					<img src="https://github.com/haihttt974.png?s=150" loading="lazy" width="150" height="150">
	        <br>
	        <a href="https://github.com/haihttt974">Duy Hải</a>
	        <p>
	        </p>
			</td>
			<td align="center" valign="top">
					<img src="https://github.com/nmdbn.png?s=150" loading="lazy" width="150" height="150">
	        <br>
	        <a href="https://github.com/nmdbn">Đại Lâm</a>
	        <p>
	        </p>
			</td>
      <td align="center" valign="top">
					<img src="https://github.com/tien190504.png?s=150" loading="lazy" width="150" height="150">
	        <br>
	        <a href="https://github.com/tien190504">Đức Tiến</a>
	        <p>
	        </p>
			</td>
		</tr>
	</table>
</div>

## 🤝 Contributing
Pull requests are welcome. For major changes, open an issue first to discuss what you would like to change. Please follow the coding conventions described in the project.

## 🗺️ Roadmap
- Email and push notification integrations
- User notification preferences
- Mobile-friendly UI improvements
- Additional analytics and reporting

## 🫶 Acknowledgements
- [ASP.NET Core](https://learn.microsoft.com/aspnet/core/)
- [SignalR](https://learn.microsoft.com/aspnet/core/signalr/)
- [Bootstrap](https://getbootstrap.com/) for UI components
