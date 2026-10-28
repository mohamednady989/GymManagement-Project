using AutoMapper;
using GymManagementBLL.Common;
using GymManagementBLL.Services.Interfaces;
using GymManagementBLL.ViewModels.SessionViewModels;
using GymManagementDAL.Entities;
using GymManagementDAL.Entities.Enums;
using GymManagementDAL.Repositories.Interfaces;

namespace GymManagementBLL.Services.Classes
{
    public class SessionService(IUnitOfWork unitOfWork, IMapper mapper) : ISessionService
    {
        private readonly IUnitOfWork _unitOfWork = unitOfWork;
        private readonly IMapper _mapper = mapper;

        public async Task<IEnumerable<SessionViewModel>?> GetAllSessionsAsync(CancellationToken ct = default)
        {
            var sessions = await _unitOfWork.SessionRepository.GetAllSessionsWithTrainerAndCategoryAsync(ct: ct);


            if (sessions?.Any() != true) return null;

            sessions = sessions.OrderByDescending(X => X.StartDate);
            var MappedSessions = _mapper.Map<IEnumerable<SessionViewModel>>(sessions);

            foreach (var session in MappedSessions)
            {
                session.AvailableSlots = session.Capacity - await _unitOfWork.SessionRepository.GetCountOfBookedSlotsAsync(session.Id, ct);
            }
            return MappedSessions;

        }
        public async Task<SessionViewModel?> GetSessionByIdAsync(int sessionId, CancellationToken ct = default)
        {
            var session = await _unitOfWork.SessionRepository.GetSessionWithTrainerAndCategoryAsync(sessionId, ct);

            if (session == null)
                return null;

            var MappedSession = _mapper.Map<SessionEntity, SessionViewModel>(session);
            MappedSession.AvailableSlots = MappedSession.Capacity - (await _unitOfWork.SessionRepository.GetCountOfBookedSlotsAsync(session.Id, ct));
            return MappedSession;
        }
        public async Task<UpdateSessionViewModel?> GetSessionToUpdateAsync(int sessionId, CancellationToken ct = default)
        {
            var session = await _unitOfWork.GetRepository<SessionEntity>().GetByIdAsync(sessionId, ct);
            if (session is null) return null;
            if (!await IsSessionValidForUpdatingAsync(session, ct)) return null;
            return _mapper.Map<UpdateSessionViewModel>(session);
        }
        public async Task<Result> CreateSessionAsync(CreateSessionViewModel model, CancellationToken ct = default)
        {
            if (model.EndDate <= model.StartDate)
                return Result.Validation("End date must be after start date.");

            if (model.StartDate <= DateTime.Now)
                return Result.Validation("Start date must be in the future.");

            var trainerRepo = _unitOfWork.GetRepository<TrainerEntity>();

            var trainer = await trainerRepo.GetByIdAsync(model.TrainerId, ct);

            if (trainer is null)
                return Result.NotFound("Trainer not found.");

            var categoryRepo = _unitOfWork.GetRepository<CategoryEntity>();
            var category = await categoryRepo.GetByIdAsync(model.CategoryId, ct);

            if (category is null)
                return Result.NotFound("Category not found.");

            // Trainer.Specialties must match the session CategoryId.
            // Compare by underlying integer values to avoid relying on enum names
            // (category names such as "Cardio" or "Strength" may not match enum identifiers).
            if ((int)trainer.Specialties != category.Id)
                return Result.Validation("Cannot create this session for this trainer.");

            var session = _mapper.Map<SessionEntity>(model);

            var sessionRepo = _unitOfWork.GetRepository<SessionEntity>();
            sessionRepo.Add(session);

            var affectedRows = await _unitOfWork.SaveChangesAsync(ct);

            return affectedRows > 0 ? Result.Ok() : Result.Fail("Failed to create session.");
        }
        public async Task<Result> UpdateSessionAsync(
       int id,
       UpdateSessionViewModel model,
       CancellationToken ct = default)
        {
            var sessionRepo = _unitOfWork.GetRepository<SessionEntity>();
            var session = await sessionRepo.GetByIdAsync(id, ct);

            if (session is null)
                return Result.NotFound("Session not found.");

            if (session.StartDate <= DateTime.Now)
                return Result.Fail("Cannot edit a session that has already started.");

            var bookedCount = await _unitOfWork.SessionRepository.GetCountOfBookedSlotsAsync(id, ct);

            if (bookedCount > 0)
                return Result.Fail("Cannot edit a session that already has bookings.");

            if (model.EndDate <= model.StartDate)
                return Result.Validation("End date must be after start date.");

            if (model.StartDate <= DateTime.Now)
                return Result.Validation(
                    "Start date must be in the future.");

            var trainerRepo = _unitOfWork.GetRepository<TrainerEntity>();

            var trainer = await trainerRepo.GetByIdAsync(model.TrainerId, ct);

            if (trainer is null)
                return Result.NotFound("Trainer not found.");

            var categoryRepo = _unitOfWork.GetRepository<CategoryEntity>();
            var category = await categoryRepo.GetByIdAsync(session.CategoryId, ct);

            if (category is null)
                return Result.NotFound("Category not found.");

            // Ensure trainer specialty matches the session category by comparing underlying int values.
            if ((int)trainer.Specialties != category.Id)
            {
                return Result.Validation("This trainer does not match the session category.");
            }

            _mapper.Map(model, session);

            session.UpdatedAt = DateTime.Now;

            sessionRepo.Update(session);

            var affectedRows = await _unitOfWork.SaveChangesAsync(ct);

            return affectedRows > 0 ? Result.Ok() : Result.Fail("Failed to update session.");
        }
        public async Task<Result> RemoveSessionAsync(int sessionId, CancellationToken ct = default)
        {
            var repo = _unitOfWork.GetRepository<SessionEntity>();
            var session = await repo.GetByIdAsync(sessionId, ct);
            if (session is null) return Result.NotFound("Session not found.");

            // Allow admins to delete sessions regardless of end date. Keep booking check below.

            var bookedCount = await _unitOfWork.SessionRepository.GetCountOfBookedSlotsAsync(sessionId, ct);
            if (bookedCount > 0)
                return Result.Fail("Cannot delete a session that has bookings.");

            repo.Delete(session);
            var affectedRows = await _unitOfWork.SaveChangesAsync(ct);

            return affectedRows > 0 ? Result.Ok() : Result.Fail("Failed to Delete session.");
        }
        public async Task<IEnumerable<TrainerSelectViewModel>> GetTrainersForDropDownAsync(CancellationToken ct = default)
        {
            var trainers = await _unitOfWork.GetRepository<TrainerEntity>().GetAllAsync(ct: ct);
            return _mapper.Map<IEnumerable<TrainerSelectViewModel>>(trainers);
        }

        public async Task<IEnumerable<CategorySelectViewModel>> GetCategoriesForDropDownAsync(CancellationToken ct = default)
        {
            var categories = await _unitOfWork.GetRepository<CategoryEntity>().GetAllAsync(ct: ct);
            return _mapper.Map<List<CategorySelectViewModel>>(categories); ;
        }


        #region Helper Methods
        private async Task<bool> IsSessionValidForUpdatingAsync(SessionEntity session, CancellationToken ct = default)
        {
            if (session.StartDate <= DateTime.Now) return false;
            var booked = await _unitOfWork.SessionRepository.GetCountOfBookedSlotsAsync(session.Id, ct);
            return booked == 0;
        }
        #endregion
    }
}