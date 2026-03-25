using SalesManagement.DAL.Entities;
using SalesManagement.BLL.Interfaces;
using SalesManagement.DAL.Interfaces;

namespace SalesManagement.BLL.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly ICustomerRepository _customerRepository;

        public AuthService(IUserRepository userRepository, ICustomerRepository customerRepository)
        {
            _userRepository = userRepository;
            _customerRepository = customerRepository;
        }




        public async Task<User?> ValidateUser(string username, string password)
        {
            var user = await _userRepository.GetByUsernameAsync(username);

            if (user == null || !user.IsActive) return null;

            bool isValid = false;
            try
            {
                isValid = BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
            }
            catch (BCrypt.Net.SaltParseException)
            {
                if (user.PasswordHash == password)
                {
                    isValid = true;
                    user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);
                }
            }

            if (!isValid) return null;

            user.LastLogin = DateTime.Now;
            _userRepository.Update(user);
            await _userRepository.SaveAsync();

            return user;
        }

        public async Task<User?> RegisterUser(
            string username, 
            string email, 
            string password, 
            string? fullName,
            string? phoneNumber)
        {
            if (await _userRepository.GetByUsernameAsync(username) != null) return null;
            if (await _userRepository.GetByEmailAsync(email) != null) return null;

            var user = new User
            {
                Username = username,
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                FullName = fullName,
                PhoneNumber = phoneNumber,
                Role = "Customer",
                IsActive = true,
                IsDeleted = false,
                CreatedDate = DateTime.Now,
                UpdatedDate = DateTime.Now
            };

            await _userRepository.AddAsync(user);
            await _userRepository.SaveAsync();

            var customer = new Customer
            {
                UserId = user.UserId,
                FullName = user.FullName ?? user.Username,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                CreatedDate = DateTime.Now,
                Type = "Personal",
                CustomerLevel = "Regular"
            };
            
            await _customerRepository.AddAsync(customer);
            await _customerRepository.SaveAsync();

            return user;
        }
    }
}
