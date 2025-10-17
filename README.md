# StarEvents - Online Event Ticketing Web Application

StarEvents is a full-featured ASP.NET MVC web application for discovering, booking, and managing events online. Designed for both customers and organizers, StarEvents makes event management and ticketing seamless, secure, and user-friendly.

## Features

- **Customer Portal**
  - Browse and search upcoming events
  - Book tickets and manage bookings
  - View booking history and loyalty points
  - Update profile and account settings

- **Organizer Portal**
  - Create, edit, and manage events
  - View event bookings and manage attendees
  - Access organizer account and security settings

- **Admin Panel**
  - Manage users, events, and platform settings

- **Security**
  - Secure authentication and password management
  - User roles: Customer, Organizer, Admin

- **Other Highlights**
  - Modern, responsive UI
  - SweetAlert notifications
  - Built with ASP.NET MVC, Entity Framework, C#, and SQL Server

## Getting Started

### 1. Clone the repository
```bash
git clone https://github.com/yourusername/StarEvents-Online-Event-Ticketing-Web-Application.git
```

### 2. Open in Visual Studio
- Open the `.sln` file with Visual Studio 2019/2022.

### 3. Database Configuration

#### Option 1: Use Entity Framework (Recommended)
- The project uses **Entity Framework Database-First** approach.
- The Entity Data Model (`StarEventsModel.edmx`) is already generated from the database schema.
- If you want to create the database from scratch:
    1. Open SQL Server Management Studio (SSMS).
    2. Run the provided [`schema.sql`](schema.sql) file to create all tables and relationships.
    3. Update the connection string in `Web.config` to point to your SQL Server instance and database name, for example:
        ```xml
        <connectionStrings>
            <add name="StarEventsDBEntities" connectionString="metadata=res://*/Models.StarEventsModel.csdl|res://*/Models.StarEventsModel.ssdl|res://*/Models.StarEventsModel.msl;provider=System.Data.SqlClient;provider connection string=&quot;data source=YOUR_SERVER;initial catalog=StarEventsDB;integrated security=True;MultipleActiveResultSets=True;App=EntityFramework&quot;" providerName="System.Data.EntityClient" />
        </connectionStrings>
        ```
    4. If you make schema changes, right-click the `.edmx` file and choose **Update Model from Database**.

#### Option 2: Use Your Own Database
- If you have an existing SQL Server database, update the connection string in `Web.config` to point to your database.
- Make sure your schema matches the [`schema.sql`](schema.sql) file provided or update the Entity Data Model as needed.

### 4. Build and Run the Project
- Press `F5` or use the "Start" button in Visual Studio.

---

## Contributing

Pull requests are welcome! For major changes, please open an issue first to discuss your ideas.

## License

This project is for educational and demonstration purposes.
