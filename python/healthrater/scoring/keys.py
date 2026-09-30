class Keys:
    SEX = "sex"
    AGE = "age"
    HEIGHT = "height"
    WEIGHT = "weight"
    WAIST = "waist"
    HIP = "hip"
    BODY_FAT = "bodyFat"
    BMI = "bmi"
    WHTR = "whtr"
    WHR = "whr"
    RESTING_HEART_RATE = "restingHeartRate"
    HEART_RATE_RECOVERY = "heartRateRecovery"
    BLOOD_PRESSURE = "bloodPressure"
    ENERGY_LEVEL = "energyLevel"
    ENERGY_STABILITY = "energyStability"
    SLEEP_QUALITY = "sleepQuality"
    CIRCADIAN_HEALTH = "circadianHealth"
    MOOD = "mood"
    MOOD_STABILITY = "moodStability"
    SOCIAL_LIFE = "socialLife"
    JOB_SATISFACTION = "jobSatisfaction"
    HOME_FAMILY_SATISFACTION = "homeFamilySatisfaction"
    HYDRATION = "hydration"
    DIGESTION = "digestion"
    IMMUNE_HEALTH = "immuneHealth"
    CAFFEINE = "caffeine"
    JUNK_FOOD = "junkFood"
    OVEREATING = "overeating"
    ALCOHOL = "alcohol"
    TOBACCO = "tobacco"
    DRUGS = "drugs"
    # Legacy v1 (39-parameter) combined parameter; no longer scored.
    SUBSTANCE_USE = "substanceUse"
    VEGETABLES_FIBER = "vegetablesFiber"
    NEAT = "neat"
    PHYSICAL_TRAINING = "physicalTraining"
    FUNCTIONAL_POWER = "functionalPower"
    COOPER = "cooper"
    SKIN_HEALTH = "skinHealth"
    JAW_SKULL_HEALTH = "jawSkullHealth"
    DENTAL_HEALTH = "dentalHealth"
    SPINAL_HEALTH = "spinalHealth"
    HAIR_HEALTH = "hairHealth"


# Single source of truth for the scored parameter set (mirrors C# ParameterSet).
PARAMETER_SET_VERSION = "v2-41"
LEGACY_PARAMETER_SET_VERSION = "v1-39"
MAX_SCORE_PER_PARAMETER = 10
PARAMETER_KEYS = [
    Keys.SEX, Keys.AGE, Keys.HEIGHT, Keys.WEIGHT,
    Keys.WAIST, Keys.HIP, Keys.BODY_FAT, Keys.BMI, Keys.WHTR, Keys.WHR,
    Keys.RESTING_HEART_RATE, Keys.HEART_RATE_RECOVERY, Keys.BLOOD_PRESSURE,
    Keys.ENERGY_LEVEL, Keys.ENERGY_STABILITY, Keys.SLEEP_QUALITY, Keys.CIRCADIAN_HEALTH,
    Keys.MOOD, Keys.MOOD_STABILITY, Keys.SOCIAL_LIFE, Keys.JOB_SATISFACTION, Keys.HOME_FAMILY_SATISFACTION,
    Keys.HYDRATION, Keys.DIGESTION, Keys.IMMUNE_HEALTH, Keys.CAFFEINE, Keys.JUNK_FOOD, Keys.OVEREATING,
    Keys.ALCOHOL, Keys.TOBACCO, Keys.DRUGS, Keys.VEGETABLES_FIBER,
    Keys.NEAT, Keys.PHYSICAL_TRAINING, Keys.FUNCTIONAL_POWER, Keys.COOPER,
    Keys.SKIN_HEALTH, Keys.JAW_SKULL_HEALTH, Keys.DENTAL_HEALTH, Keys.SPINAL_HEALTH, Keys.HAIR_HEALTH,
]
TOTAL_PARAMETER_COUNT = len(PARAMETER_KEYS)
MAX_TOTAL_SCORE = TOTAL_PARAMETER_COUNT * MAX_SCORE_PER_PARAMETER
