using ScheduleMaker.App.Domain;

namespace ScheduleMaker.App.Application;

public static class ScheduleGenerationService
{
    public static ApplicationOperationResult<GeneratedSchedule> Generate(ApplicationState state)
    {
        var validation = ConfigurationValidation.Validate(state);
        if (!validation.CanGenerate)
        {
            return ApplicationOperationResult<GeneratedSchedule>.Failure(
                validation.NextStepMessage ?? "Configuration is not ready for schedule generation.");
        }

        var participants = state.Participants
            .OrderBy(p => p.SortOrder)
            .ToList();

        // Track total assignments per participant (by index in participants list)
        var totalCounts = new int[participants.Count];

        // Track per-task counts: taskDefinitionId -> participant index -> count
        var taskCounts = new Dictionary<Guid, int[]>();

        // Build lookup maps
        var eventTypeById = state.EventTypes.ToDictionary(et => et.Id);

        // Process events in chronological order, preserving existing stable ordering
        var generatedEvents = new List<GeneratedScheduleEvent>();

        foreach (var scheduledEvent in state.ScheduledEvents)
        {
            if (!eventTypeById.TryGetValue(scheduledEvent.EventTypeId, out var eventType))
            {
                continue;
            }

            var assignments = new List<GeneratedTaskAssignment>();

            foreach (var task in eventType.Tasks.OrderBy(t => t.SortOrder))
            {
                if (!taskCounts.TryGetValue(task.Id, out var counts))
                {
                    counts = new int[participants.Count];
                    taskCounts[task.Id] = counts;
                }

                // Find the minimum total assignment count across all participants
                var minTotal = int.MaxValue;
                for (var i = 0; i < participants.Count; i++)
                {
                    if (totalCounts[i] < minTotal)
                    {
                        minTotal = totalCounts[i];
                    }
                }

                // Among participants with minTotal, select by lowest task count, then stable order
                var selectedIndex = -1;
                var minTaskCount = int.MaxValue;
                for (var i = 0; i < participants.Count; i++)
                {
                    if (totalCounts[i] == minTotal && counts[i] < minTaskCount)
                    {
                        minTaskCount = counts[i];
                        selectedIndex = i;
                    }
                }

                var selected = participants[selectedIndex];
                totalCounts[selectedIndex]++;
                counts[selectedIndex]++;

                assignments.Add(new GeneratedTaskAssignment(
                    task.Id,
                    task.Name,
                    selected.Id,
                    selected.Name));
            }

            generatedEvents.Add(new GeneratedScheduleEvent(
                scheduledEvent.Id,
                scheduledEvent.Date,
                eventType.Id,
                eventType.Name,
                scheduledEvent.Description,
                assignments));
        }

        var participantTotals = participants
            .Select((p, i) => new ParticipantAssignmentTotal(p.Id, p.Name, totalCounts[i]))
            .ToList();

        var schedule = new GeneratedSchedule(
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            generatedEvents,
            participantTotals);

        return ApplicationOperationResult<GeneratedSchedule>.Success(schedule);
    }
}
