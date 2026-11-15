using FitSphere.Models;

namespace FitSphere.Data;

/// <summary>Built-in exercise library (inserted once on first launch).</summary>
public static class SeedData
{
    public static List<Exercise> Exercises()
    {
        var list = new List<Exercise>();
        void Add(string name, string group, string equip, bool compound, string steps) =>
            list.Add(new Exercise { Name = name, MuscleGroup = group, Equipment = equip, IsCompound = compound, Instructions = steps });

        // ---------- Chest ----------
        Add("Barbell Bench Press", "Chest", "Barbell", true, "Lie on a flat bench with feet planted and shoulder blades pinched together.|Grip the bar slightly wider than shoulder width and unrack it over your chest.|Lower the bar under control to mid-chest.|Press up until your arms are straight.");
        Add("Dumbbell Bench Press", "Chest", "Dumbbell", true, "Sit on a bench with dumbbells on your thighs, then lie back and press them over your chest.|Lower the weights until your elbows are just below bench level.|Press back up and slightly together without locking out hard.");
        Add("Incline Dumbbell Press", "Chest", "Dumbbell", true, "Set a bench to a 30-45 degree incline.|Press the dumbbells from shoulder level to above your upper chest.|Lower slowly and keep your wrists stacked over your elbows.");
        Add("Push-Up", "Chest", "Bodyweight", true, "Place hands slightly wider than shoulders with a straight line from head to heels.|Lower your chest to just above the floor.|Push the floor away until your arms are straight.");
        Add("Incline Push-Up", "Chest", "Bodyweight", true, "Place your hands on a bench or sturdy surface.|Keep your body straight and lower your chest to the edge.|Press back up. Use a lower surface to make it harder.");
        Add("Machine Chest Press", "Chest", "Machine", true, "Adjust the seat so the handles are at mid-chest height.|Press the handles forward until your arms are almost straight.|Return slowly without letting the weight stack touch down.");
        Add("Cable Fly", "Chest", "Cable", false, "Set both pulleys at chest height and stand in the middle with a slight forward lean.|With a soft bend in your elbows, bring the handles together in front of your chest.|Open your arms slowly until you feel a stretch across the chest.");
        Add("Dumbbell Fly", "Chest", "Dumbbell", false, "Lie on a flat bench with dumbbells above your chest, palms facing each other.|Lower the weights out to the sides in a wide arc with soft elbows.|Squeeze your chest to bring them back together.");
        Add("Chest Dip", "Chest", "Bodyweight", true, "Support yourself on parallel bars and lean your torso forward.|Lower until your upper arms are about parallel to the floor.|Press up to the start position.");

        // ---------- Back ----------
        Add("Pull-Up", "Back", "Bodyweight", true, "Hang from a bar with an overhand grip slightly wider than shoulders.|Pull your chest toward the bar by driving your elbows down.|Lower yourself fully under control.");
        Add("Lat Pulldown", "Back", "Machine", true, "Sit with your thighs secured under the pad and grip the bar wider than shoulders.|Pull the bar to your upper chest while keeping your torso tall.|Return the bar slowly until your arms are straight.");
        Add("Barbell Row", "Back", "Barbell", true, "Hinge at the hips with a flat back and the bar hanging below your knees.|Row the bar to your lower ribs.|Lower it under control without rounding your back.");
        Add("One-Arm Dumbbell Row", "Back", "Dumbbell", true, "Place one hand and knee on a bench with a flat back.|Row the dumbbell toward your hip, keeping your elbow close.|Lower until your arm is fully extended.");
        Add("Seated Cable Row", "Back", "Cable", true, "Sit with feet braced and knees slightly bent.|Pull the handle to your midsection while squeezing your shoulder blades together.|Extend your arms slowly without leaning too far forward.");
        Add("Machine Row", "Back", "Machine", true, "Adjust the chest pad so you can reach the handles with straight arms.|Pull the handles back until your elbows pass your torso.|Return slowly and keep your chest on the pad.");
        Add("Inverted Row", "Back", "Bodyweight", true, "Lie under a low bar and hold it with arms straight and body in a plank line.|Pull your chest up to the bar.|Lower under control. Bend your knees to make it easier.");
        Add("Deadlift", "Back", "Barbell", true, "Stand with the bar over mid-foot and grip just outside your legs.|Brace your core, push the floor away and stand up with a flat back.|Lower the bar by hinging at the hips and bending the knees.");
        Add("Back Extension", "Back", "Bodyweight", false, "Set the pad just below your hips and cross your arms over your chest.|Lower your torso with a flat back.|Raise back to a straight line without over-arching.");

        // ---------- Legs ----------
        Add("Barbell Back Squat", "Legs", "Barbell", true, "Set the bar on your upper back and stand with feet shoulder-width apart.|Sit down and back until your thighs are at least parallel.|Drive up through your whole foot to stand.");
        Add("Goblet Squat", "Legs", "Dumbbell", true, "Hold a dumbbell vertically against your chest.|Squat down between your knees while keeping your chest tall.|Stand back up by driving through your heels.");
        Add("Bodyweight Squat", "Legs", "Bodyweight", true, "Stand with feet shoulder-width apart and arms in front.|Sit your hips back and down until thighs are parallel.|Stand up tall.");
        Add("Leg Press", "Legs", "Machine", true, "Sit with your back flat against the pad and feet shoulder-width on the platform.|Lower the platform until your knees reach about 90 degrees.|Press up without locking your knees.");
        Add("Romanian Deadlift", "Legs", "Barbell", true, "Hold the bar at hip level with a slight knee bend.|Push your hips back and slide the bar down your thighs.|Stand tall by squeezing your glutes.");
        Add("Dumbbell Romanian Deadlift", "Legs", "Dumbbell", true, "Hold dumbbells in front of your thighs.|Hinge back at the hips with a flat spine until you feel your hamstrings stretch.|Return to standing by driving your hips forward.");
        Add("Walking Lunge", "Legs", "Dumbbell", true, "Hold dumbbells at your sides.|Step forward and lower your back knee toward the floor.|Push through the front foot and step into the next lunge.");
        Add("Bodyweight Lunge", "Legs", "Bodyweight", true, "Stand tall and step forward with one leg.|Lower until both knees are around 90 degrees.|Push back to the start and switch legs.");
        Add("Leg Extension", "Legs", "Machine", false, "Sit with the pad resting on your lower shins.|Extend your knees until your legs are straight.|Lower slowly.");
        Add("Leg Curl", "Legs", "Machine", false, "Lie or sit with the pad just above your heels.|Curl your heels toward your glutes.|Return slowly to the start.");
        Add("Calf Raise", "Legs", "Bodyweight", false, "Stand on the edge of a step with your heels hanging off.|Rise onto your toes as high as you can.|Lower below the step level for a full stretch.");
        Add("Glute Bridge", "Legs", "Bodyweight", false, "Lie on your back with knees bent and feet flat.|Drive through your heels and lift your hips until your body is a straight line.|Squeeze at the top and lower slowly.");
        Add("Barbell Hip Thrust", "Legs", "Barbell", true, "Sit with your upper back against a bench and the bar across your hips.|Drive your hips up until your torso is parallel to the floor.|Lower under control.");

        // ---------- Shoulders ----------
        Add("Overhead Press", "Shoulders", "Barbell", true, "Hold the bar at shoulder height with a grip just outside your shoulders.|Brace your core and press the bar overhead until your arms lock out.|Lower it back to your collarbone.");
        Add("Dumbbell Shoulder Press", "Shoulders", "Dumbbell", true, "Sit or stand with dumbbells at shoulder height.|Press them overhead until your arms are straight.|Lower slowly to ear level.");
        Add("Machine Shoulder Press", "Shoulders", "Machine", true, "Adjust the seat so the handles start at shoulder height.|Press upward until your arms are almost straight.|Lower under control.");
        Add("Lateral Raise", "Shoulders", "Dumbbell", false, "Stand with dumbbells at your sides and a slight bend in the elbows.|Raise your arms out to the sides to shoulder height.|Lower slowly. Avoid swinging.");
        Add("Cable Lateral Raise", "Shoulders", "Cable", false, "Stand side-on to a low pulley and hold the handle with the far hand.|Raise your arm out to shoulder height.|Lower under control.");
        Add("Front Raise", "Shoulders", "Dumbbell", false, "Hold dumbbells in front of your thighs.|Raise one or both arms to shoulder height in front of you.|Lower slowly.");
        Add("Rear Delt Fly", "Shoulders", "Dumbbell", false, "Hinge forward with a flat back and dumbbells hanging below you.|Raise your arms out to the sides, squeezing your shoulder blades.|Lower slowly.");
        Add("Pike Push-Up", "Shoulders", "Bodyweight", true, "Start in a push-up position with hips lifted high so your body forms an inverted V.|Bend your elbows to lower the top of your head toward the floor.|Press back up.");
        Add("Face Pull", "Shoulders", "Cable", false, "Set a rope at upper-chest height.|Pull the rope toward your face, flaring your elbows out.|Return slowly with straight arms.");

        // ---------- Arms ----------
        Add("Barbell Curl", "Arms", "Barbell", false, "Stand holding the bar with an underhand grip at hip level.|Curl the bar toward your shoulders without swinging.|Lower it slowly until your arms are straight.");
        Add("Dumbbell Curl", "Arms", "Dumbbell", false, "Stand with dumbbells at your sides, palms forward.|Curl the weights up while keeping your elbows pinned.|Lower under control.");
        Add("Hammer Curl", "Arms", "Dumbbell", false, "Hold dumbbells with palms facing each other.|Curl them up without rotating your wrists.|Lower slowly.");
        Add("Cable Curl", "Arms", "Cable", false, "Stand facing a low pulley with a straight bar.|Curl the bar to your shoulders keeping elbows still.|Lower slowly.");
        Add("Triceps Pushdown", "Arms", "Cable", false, "Stand at a high pulley and grip the bar or rope with elbows at your sides.|Press down until your arms are straight.|Return slowly to chest height.");
        Add("Overhead Triceps Extension", "Arms", "Dumbbell", false, "Hold one dumbbell overhead with both hands.|Bend your elbows to lower it behind your head.|Extend your arms back up.");
        Add("Skull Crusher", "Arms", "Barbell", false, "Lie on a bench holding a bar above your chest.|Bend your elbows to lower the bar toward your forehead.|Extend your arms back up without flaring your elbows.");
        Add("Bench Dip", "Arms", "Bodyweight", false, "Place your hands on a bench behind you with legs extended.|Lower your body by bending your elbows to about 90 degrees.|Press back up.");
        Add("Close-Grip Push-Up", "Arms", "Bodyweight", true, "Place your hands under your shoulders, close together.|Lower your chest with elbows tucked against your sides.|Press back up.");

        // ---------- Core ----------
        Add("Plank", "Core", "Bodyweight", false, "Rest on your forearms and toes with your body in a straight line.|Brace your abs and squeeze your glutes.|Hold. Use the reps field as seconds.");
        Add("Crunch", "Core", "Bodyweight", false, "Lie on your back with knees bent and hands by your temples.|Curl your shoulders off the floor by contracting your abs.|Lower slowly.");
        Add("Hanging Knee Raise", "Core", "Bodyweight", false, "Hang from a bar with a firm grip.|Raise your knees toward your chest without swinging.|Lower slowly.");
        Add("Cable Crunch", "Core", "Cable", false, "Kneel facing a high pulley holding a rope by your head.|Crunch down, bringing elbows toward your knees.|Return slowly.");
        Add("Russian Twist", "Core", "Bodyweight", false, "Sit with your knees bent and torso leaned back.|Rotate your torso side to side, tapping the floor each time.|Keep your chest tall.");
        Add("Dead Bug", "Core", "Bodyweight", false, "Lie on your back with arms up and knees bent at 90 degrees.|Lower the opposite arm and leg while pressing your lower back into the floor.|Return and switch sides.");

        // ---------- Cardio ----------
        Add("Treadmill Run", "Cardio", "Machine", false, "Start with a 3-5 minute easy warm-up.|Increase to a steady pace you can hold while breathing hard but controlled.|Finish with a slow cool-down. Use reps as minutes.");
        Add("Rowing Machine", "Cardio", "Machine", true, "Push with your legs first, then lean back and pull the handle to your ribs.|Reverse the movement on the way back.|Keep a steady rhythm. Use reps as minutes.");
        Add("Stationary Bike", "Cardio", "Machine", false, "Adjust the seat so your knee is slightly bent at the bottom of the pedal stroke.|Pedal at a steady cadence.|Use reps as minutes.");
        Add("Jump Rope", "Cardio", "Bodyweight", false, "Hold the handles at hip height and turn the rope with your wrists.|Jump just high enough to clear the rope.|Land softly on the balls of your feet.");
        Add("Burpee", "Cardio", "Bodyweight", true, "Squat down and place your hands on the floor.|Jump your feet back into a plank, then jump them forward again.|Explode upward into a jump.");
        Add("Mountain Climber", "Cardio", "Bodyweight", false, "Start in a high plank position.|Drive your knees toward your chest one after the other.|Keep your hips low and core tight.");

        return list;
    }
}
