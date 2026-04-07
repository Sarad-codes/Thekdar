using Thekdar.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Thekdar.Data
{
    public static class DbSeeder
    {
        public static async Task SeedAsync(ApplicationDbContext context, ILogger? logger = null)
        {
            try
            {
                logger?.LogInformation("Checking database migrations...");
                await context.Database.MigrateAsync();
                logger?.LogInformation("Database migrations applied successfully.");

                var usedEmployeeEmails = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var usedEmployeeNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var usedEmployeePhones = new HashSet<string>(StringComparer.Ordinal);
                var usedEmployeePanNumbers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                (string FirstName, string LastName) NextUniqueEmployeeName(string firstName, string lastName)
                {
                    var normalizedFirstName = firstName.Trim();
                    var normalizedLastName = lastName.Trim();
                    var suffix = 0;

                    while (true)
                    {
                        var candidateLastName = suffix == 0
                            ? normalizedLastName
                            : $"{normalizedLastName} {suffix}";
                        var candidateFullName = $"{normalizedFirstName} {candidateLastName}";

                        if (usedEmployeeNames.Add(candidateFullName))
                        {
                            return (normalizedFirstName, candidateLastName);
                        }

                        suffix++;
                    }
                }

                string NextUniqueEmployeeEmail(string firstName, string lastName, int contractorId)
                {
                    var baseLocalPart = $"{firstName.Trim().ToLowerInvariant()}.{lastName.Trim().ToLowerInvariant()}.{contractorId}";
                    var suffix = 0;

                    while (true)
                    {
                        var localPart = suffix == 0 ? baseLocalPart : $"{baseLocalPart}.{suffix}";
                        var candidate = $"{localPart}@gmail.com";

                        if (usedEmployeeEmails.Add(candidate))
                        {
                            return candidate;
                        }

                        suffix++;
                    }
                }

                string NextUniqueEmployeePhone(string? disallowed = null)
                {
                    while (true)
                    {
                        var candidate = $"98{Random.Shared.Next(10000000, 99999999)}";

                        if (candidate != disallowed && usedEmployeePhones.Add(candidate))
                        {
                            return candidate;
                        }
                    }
                }

                string NextUniquePanNumber()
                {
                    while (true)
                    {
                        var candidate = $"PAN{Random.Shared.Next(100000000, 999999999)}";

                        if (usedEmployeePanNumbers.Add(candidate))
                        {
                            return candidate;
                        }
                    }
                }

                // ========== USERS (ADMIN & CONTRACTORS) ==========
                if (!await context.Users.AnyAsync())
                {
                    logger?.LogInformation("No users found. Creating default accounts...");

                    var users = new List<UserModel>();

                    // Admin
                    users.Add(new UserModel
                    {
                        Name = "Admin",
                        Email = "saradacharya592@gmail.com",
                        Phone = "9800000001",
                        Age = 22,
                        Role = UserRole.Admin,
                        Status = UserStatus.Active,
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword("sarad@123"),
                        CreatedAt = DateTime.UtcNow.AddMonths(-12)
                    });

                    // Contractors
                    var contractors = new[]
                    {
                        new { Name = "Hari Shrestha", Email = "hari@shresthaconstruction.com", Phone = "9841234567", Age = 42 },
                        new { Name = "Sita Gurung", Email = "sita@gurungbuilders.com", Phone = "9842345678", Age = 38 },
                        new { Name = "Ram Bahadur Thapa", Email = "ram@thapaelectrical.com", Phone = "9843456789", Age = 45 },
                        new { Name = "Gita Karki", Email = "gita@karkiplumbing.com", Phone = "9844567890", Age = 33 },
                        new { Name = "Bikram Shrestha", Email = "bikram@shresthainteriors.com", Phone = "9845678901", Age = 29 }
                    };

                    foreach (var c in contractors)
                    {
                        users.Add(new UserModel
                        {
                            Name = c.Name,
                            Email = c.Email,
                            Phone = c.Phone,
                            Age = c.Age,
                            Role = UserRole.Contractor,
                            Status = UserStatus.Active,
                            PasswordHash = BCrypt.Net.BCrypt.HashPassword("contractor@123"),
                            CreatedAt = DateTime.UtcNow.AddMonths(-Random.Shared.Next(1, 24))
                        });
                    }

                    context.Users.AddRange(users);
                    await context.SaveChangesAsync();
                    logger?.LogInformation($"Seeded {users.Count} users (1 Admin, {contractors.Length} Contractors).");
                }

                // ========== EMPLOYEES (WORKERS) ==========
                if (!await context.Employees.AnyAsync())
                {
                    logger?.LogInformation("Seeding employees (workers)...");

                    var contractors = await context.Users.Where(u => u.Role == UserRole.Contractor).ToListAsync();
                    var employees = new List<EmployeeModel>();

                    var workerNames = new[]
                    {
                        new { First = "Ram", Last = "Prasad", Trade = "Plumber", Rate = 650 },
                        new { First = "Sita", Last = "Karki", Trade = "Electrician", Rate = 750 },
                        new { First = "Gopal", Last = "Dahal", Trade = "Carpenter", Rate = 700 },
                        new { First = "Mina", Last = "Gurung", Trade = "Mason", Rate = 800 },
                        new { First = "Krishna", Last = "Poudel", Trade = "Painter", Rate = 600 },
                        new { First = "Bishnu", Last = "Adhikari", Trade = "Plumber", Rate = 700 },
                        new { First = "Laxmi", Last = "Tamang", Trade = "Electrician", Rate = 800 },
                        new { First = "Prakash", Last = "Neupane", Trade = "Carpenter", Rate = 750 },
                        new { First = "Sunita", Last = "Bhandari", Trade = "Mason", Rate = 850 },
                        new { First = "Rajesh", Last = "Shrestha", Trade = "Painter", Rate = 650 },
                        new { First = "Anita", Last = "Rai", Trade = "Plumber", Rate = 680 },
                        new { First = "Dipak", Last = "Magar", Trade = "Electrician", Rate = 780 },
                        new { First = "Sarita", Last = "Thapa", Trade = "Carpenter", Rate = 720 },
                        new { First = "Mohan", Last = "KC", Trade = "Mason", Rate = 820 },
                        new { First = "Nirmala", Last = "Shahi", Trade = "Painter", Rate = 620 }
                    };

                    foreach (var contractor in contractors)
                    {
                        // Assign 3 workers per contractor
                        var assignedWorkers = workerNames.Skip(Random.Shared.Next(0, workerNames.Length - 5)).Take(3);
                        foreach (var w in assignedWorkers)
                        {
                            var (firstName, lastName) = NextUniqueEmployeeName(w.First, w.Last);
                            var primaryPhone = NextUniqueEmployeePhone();
                            var secondaryPhone = Random.Shared.Next(0, 2) == 0
                                ? null
                                : NextUniqueEmployeePhone(primaryPhone);

                            employees.Add(new EmployeeModel
                            {
                                FirstName = firstName,
                                LastName = lastName,
                                Trade = w.Trade,
                                Phone1 = primaryPhone,
                                Phone2 = secondaryPhone,
                                Email = NextUniqueEmployeeEmail(firstName, lastName, contractor.Id),
                                DailyRate = w.Rate,
                                IsAvailable = Random.Shared.Next(0, 5) != 0, // 80% available
                                PanNumber = NextUniquePanNumber(),
                                PanCardImagePath = null,
                                ProfilePicturePath = null,
                                HireDate = DateTime.UtcNow.AddMonths(-Random.Shared.Next(1, 48)),
                                ContractorId = contractor.Id,
                                IsDeleted = false
                            });
                        }
                    }

                    context.Employees.AddRange(employees);
                    await context.SaveChangesAsync();
                    logger?.LogInformation($"Seeded {employees.Count} employees.");
                }

                // ========== JOBS ==========
                if (!await context.Jobs.AnyAsync())
                {
                    logger?.LogInformation("Seeding jobs...");

                    var contractors = await context.Users.Where(u => u.Role == UserRole.Contractor).ToListAsync();
                    var jobs = new List<JobModel>();

                    var jobTemplates = new[]
                    {
                        new { Title = "Bathroom Renovation", Client = "Shree Krishna Plywood", Address = "Baneshwor, Kathmandu", Trade = "Plumber" },
                        new { Title = "Water Heater Installation", Client = "Sushila Rana", Address = "Kapan, Kathmandu", Trade = "Plumber" },
                        new { Title = "Leakage Repair", Client = "Sunita Sharma", Address = "Basundhara, Kathmandu", Trade = "Plumber" },
                        new { Title = "Toilet Installation", Client = "Ramesh Khadka", Address = "Budhanilkantha, Kathmandu", Trade = "Plumber" },
                        new { Title = "Drain Cleaning", Client = "Thamel House Restaurant", Address = "Thamel, Kathmandu", Trade = "Plumber" },
                        new { Title = "New Electrical Wiring", Client = "Ram Bahadur Thapa", Address = "Patan, Lalitpur", Trade = "Electrician" },
                        new { Title = "Generator Service", Client = "Nepal Pharmacy", Address = "Maharajgunj, Kathmandu", Trade = "Electrician" },
                        new { Title = "Lighting Installation", Client = "Cafe Soma", Address = "Jhamsikhel, Lalitpur", Trade = "Electrician" },
                        new { Title = "Solar Panel Installation", Client = "Bhaktapur Eco-Home", Address = "Bhaktapur", Trade = "Electrician" },
                        new { Title = "Smart Home Wiring", Client = "Tech Innovations", Address = "Naxal, Kathmandu", Trade = "Electrician" },
                        new { Title = "Custom Kitchen Cabinet", Client = "Rita Shrestha", Address = "Lazimpat, Kathmandu", Trade = "Carpenter" },
                        new { Title = "Wooden Deck Repair", Client = "Patan Garden Restaurant", Address = "Patan, Lalitpur", Trade = "Carpenter" },
                        new { Title = "Furniture Restoration", Client = "Boudha Antiques", Address = "Boudha, Kathmandu", Trade = "Carpenter" },
                        new { Title = "House Painting", Client = "Newa House", Address = "Bhaktapur", Trade = "Painter" },
                        new { Title = "Wall Mural", Client = "Art Cafe", Address = "Lakeside, Pokhara", Trade = "Painter" },
                        new { Title = "Building Construction", Client = "Nepal Housing", Address = "Tinkune, Kathmandu", Trade = "Mason" },
                        new { Title = "Retaining Wall", Client = "Himalayan Bank", Address = "New Road, Pokhara", Trade = "Mason" }
                    };

                    foreach (var contractor in contractors)
                    {
                        for (int i = 0; i < 8; i++) // 8 jobs per contractor
                        {
                            var template = jobTemplates[Random.Shared.Next(jobTemplates.Length)];
                            var status = Random.Shared.Next(0, 10) switch
                            {
                                < 6 => JobStatus.Active,
                                < 8 => JobStatus.PendingConfirmation,
                                _ => JobStatus.Completed
                            };

                            var scheduledDate = status == JobStatus.Completed
                                ? DateTime.UtcNow.AddDays(-Random.Shared.Next(1, 60))
                                : DateTime.UtcNow.AddDays(Random.Shared.Next(-2, 30));

                            jobs.Add(new JobModel
                            {
                                Title = $"{template.Title} - {contractor.Name.Split(' ')[0]}",
                                Description = $"Complete {template.Title.ToLower()} work at {template.Address}. Quality workmanship required.",
                                ClientName = template.Client,
                                Address = template.Address,
                                ScheduledDate = scheduledDate,
                                Status = status,
                                CreatedByUserId = contractor.Id,
                                CreatedAt = DateTime.UtcNow.AddDays(-Random.Shared.Next(1, 90)),
                                CompletedByUserId = status == JobStatus.Completed ? contractor.Id : null
                            });
                        }
                    }

                    context.Jobs.AddRange(jobs);
                    await context.SaveChangesAsync();
                    logger?.LogInformation($"Seeded {jobs.Count} jobs.");
                }

                // ========== JOB ASSIGNMENTS ==========
                if (!await context.JobAssignments.AnyAsync())
                {
                    logger?.LogInformation("Seeding job assignments...");

                    var jobs = await context.Jobs.Include(j => j.CreatedBy).ToListAsync();
                    var employees = await context.Employees.ToListAsync();

                    if (jobs.Any() && employees.Any())
                    {
                        var assignments = new List<JobAssignmentModel>();

                        foreach (var job in jobs)
                        {
                            // Get employees that match the job's trade (based on title keywords)
                            var matchingEmployees = employees.Where(e =>
                                (job.Title.Contains("Plumber") || job.Title.Contains("Water") || job.Title.Contains("Leakage") || job.Title.Contains("Toilet") || job.Title.Contains("Drain")) && e.Trade == "Plumber" ||
                                (job.Title.Contains("Electrical") || job.Title.Contains("Wiring") || job.Title.Contains("Generator") || job.Title.Contains("Lighting") || job.Title.Contains("Solar")) && e.Trade == "Electrician" ||
                                (job.Title.Contains("Cabinet") || job.Title.Contains("Deck") || job.Title.Contains("Furniture")) && e.Trade == "Carpenter" ||
                                (job.Title.Contains("Paint") || job.Title.Contains("Mural")) && e.Trade == "Painter" ||
                                (job.Title.Contains("Construction") || job.Title.Contains("Wall")) && e.Trade == "Mason"
                            ).ToList();

                            // If no specific match, take random employees
                            if (!matchingEmployees.Any())
                            {
                                matchingEmployees = employees.Take(Random.Shared.Next(1, 4)).ToList();
                            }

                            // Assign 1-3 employees per job
                            var assignedCount = Random.Shared.Next(1, Math.Min(4, matchingEmployees.Count + 1));
                            var selected = matchingEmployees.OrderBy(x => Guid.NewGuid()).Take(assignedCount).ToList();

                            foreach (var emp in selected)
                            {
                                assignments.Add(new JobAssignmentModel
                                {
                                    JobId = job.Id,
                                    EmployeeId = emp.Id,
                                    Role = assignedCount == 1 ? "Lead" : (emp == selected.First() ? "Lead" : "Assistant"),
                                    AssignedDate = job.CreatedAt.AddDays(Random.Shared.Next(1, 5)),
                                    Status = job.Status == JobStatus.Completed ? "Completed" : "Assigned"
                                });
                            }
                        }

                        if (assignments.Any())
                        {
                            context.JobAssignments.AddRange(assignments);
                            await context.SaveChangesAsync();
                            logger?.LogInformation($"Seeded {assignments.Count} job assignments.");
                        }
                    }
                }

                // ========== PAYROLL RECORDS ==========
                if (!await context.Payrolls.AnyAsync())
                {
                    logger?.LogInformation("Seeding payroll records...");

                    var employeesList = await context.Employees.Include(e => e.Contractor).ToListAsync();
                    var payrolls = new List<PayrollModel>();

                    foreach (var employee in employeesList)
                    {
                        // Create 4-6 payroll records per employee (past 6 months)
                        var recordCount = Random.Shared.Next(4, 7);
                        var currentDate = DateTime.UtcNow;

                        for (int i = 0; i < recordCount; i++)
                        {
                            var periodEnd = currentDate.AddMonths(-i);
                            var periodStart = new DateTime(periodEnd.Year, periodEnd.Month, 1);
                            var daysWorked = Random.Shared.Next(15, 27);
                            var overtimeHours = Random.Shared.Next(0, 12);
                            var overtimeMultiplier = Random.Shared.Next(1, 4) switch
                            {
                                1 => 1.5m,
                                2 => 2m,
                                _ => 3m
                            };
                            var bonus = Random.Shared.Next(0, 5000);
                            var deduction = Random.Shared.Next(0, 3000);

                            var dailyRate = employee.DailyRate;
                            var hourlyRate = dailyRate / 8;
                            var baseWage = dailyRate * daysWorked;
                            var overtimeWage = hourlyRate * overtimeHours * overtimeMultiplier;
                            var totalWage = baseWage + overtimeWage + bonus;
                            var netPayable = totalWage - deduction;

                            // Determine payment status
                            var status = periodEnd >= DateTime.UtcNow.AddMonths(-1)
                                ? PaymentStatus.Pending
                                : (Random.Shared.Next(0, 3) == 0 ? PaymentStatus.Pending : PaymentStatus.Paid);

                            var paymentDate = status == PaymentStatus.Paid
                                ? periodEnd.AddDays(Random.Shared.Next(5, 15))
                                : (DateTime?)null;

                            payrolls.Add(new PayrollModel
                            {
                                EmployeeId = employee.Id,
                                ContractorId = employee.ContractorId,
                                PeriodStart = periodStart,
                                PeriodEnd = periodEnd,
                                DaysWorked = daysWorked,
                                DailyRate = dailyRate,
                                BaseWage = baseWage,
                                OvertimeHours = overtimeHours,
                                OvertimeMultiplier = overtimeMultiplier,
                                OvertimeWage = overtimeWage,
                                Bonus = bonus,
                                BonusReason = bonus > 0 ? (Random.Shared.Next(0, 2) == 0 ? "Performance bonus" : "Festival bonus") : null,
                                Deduction = deduction,
                                DeductionReason = deduction > 0 ? (Random.Shared.Next(0, 2) == 0 ? "Loan advance" : "Material deduction") : null,
                                TotalWage = totalWage,
                                NetPayable = netPayable,
                                Status = status,
                                PaymentDate = paymentDate,
                                PaymentMethod = paymentDate.HasValue ? (Random.Shared.Next(0, 2) == 0 ? "Cash" : "Bank Transfer") : null,
                                TransactionReference = paymentDate.HasValue ? $"TXN{DateTime.Now:yyyyMMdd}{Random.Shared.Next(1000, 9999)}" : null,
                                Notes = Random.Shared.Next(0, 3) == 0 ? "Regular monthly payroll" : null,
                                CreatedByUserId = employee.ContractorId,
                                CreatedAt = periodEnd.AddDays(5),
                                PayslipGenerated = true,
                                PayslipNumber = $"PS-{periodEnd:yyyyMM}-{employee.Id:D6}"
                            });
                        }
                    }

                    context.Payrolls.AddRange(payrolls);
                    await context.SaveChangesAsync();
                    logger?.LogInformation($"Seeded {payrolls.Count} payroll records.");
                }

                logger?.LogInformation("========== DATABASE SEEDING COMPLETED SUCCESSFULLY ==========");
                logger?.LogInformation($"Users: {await context.Users.CountAsync()}");
                logger?.LogInformation($"Employees: {await context.Employees.CountAsync()}");
                logger?.LogInformation($"Jobs: {await context.Jobs.CountAsync()}");
                logger?.LogInformation($"Job Assignments: {await context.JobAssignments.CountAsync()}");
                logger?.LogInformation($"Payrolls: {await context.Payrolls.CountAsync()}");
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Error seeding database");
                throw;
            }
        }
    }
}

