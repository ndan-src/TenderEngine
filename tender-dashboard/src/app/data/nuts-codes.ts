/**
 * NUTS code → Human-readable region name lookup for Germany (DE).
 * Covers NUTS1 (Länder), NUTS2 (Regierungsbezirke), and NUTS3 (Kreise/kreisfreie Städte).
 * Non-German codes fall back to the code itself.
 */
export const NUTS_LABELS: Record<string, string> = {
  // NUTS1 — Bundesländer
  DE1:  'Baden-Württemberg',
  DE2:  'Bavaria',
  DE3:  'Berlin',
  DE4:  'Brandenburg',
  DE5:  'Bremen',
  DE6:  'Hamburg',
  DE7:  'Hesse',
  DE8:  'Mecklenburg-Vorpommern',
  DE9:  'Lower Saxony',
  DEA:  'North Rhine-Westphalia',
  DEB:  'Rhineland-Palatinate',
  DEC:  'Saarland',
  DED:  'Saxony',
  DEE:  'Saxony-Anhalt',
  DEF:  'Schleswig-Holstein',
  DEG:  'Thuringia',

  // NUTS2 — Baden-Württemberg
  DE11: 'Stuttgart Region',
  DE12: 'Karlsruhe Region',
  DE13: 'Freiburg Region',
  DE14: 'Tübingen Region',

  // NUTS2 — Bavaria
  DE21: 'Oberbayern',
  DE22: 'Niederbayern',
  DE23: 'Oberpfalz',
  DE24: 'Oberfranken',
  DE25: 'Mittelfranken',
  DE26: 'Unterfranken',
  DE27: 'Schwaben',

  // NUTS2 — Berlin
  DE30: 'Berlin',

  // NUTS2 — Brandenburg
  DE40: 'Brandenburg',

  // NUTS2 — Bremen
  DE50: 'Bremen',

  // NUTS2 — Hamburg
  DE60: 'Hamburg',

  // NUTS2 — Hesse
  DE71: 'Darmstadt Region',
  DE72: 'Gießen Region',
  DE73: 'Kassel Region',

  // NUTS2 — Mecklenburg-Vorpommern
  DE80: 'Mecklenburg-Vorpommern',

  // NUTS2 — Lower Saxony
  DE91: 'Braunschweig Region',
  DE92: 'Hannover Region',
  DE93: 'Lüneburg Region',
  DE94: 'Weser-Ems Region',

  // NUTS2 — North Rhine-Westphalia
  DEA1: 'Düsseldorf Region',
  DEA2: 'Cologne Region',
  DEA3: 'Münster Region',
  DEA4: 'Detmold Region',
  DEA5: 'Arnsberg Region',

  // NUTS2 — Rhineland-Palatinate
  DEB1: 'Koblenz Region',
  DEB2: 'Trier Region',
  DEB3: 'Rheinhessen-Pfalz Region',

  // NUTS2 — Saarland
  DEC0: 'Saarland',

  // NUTS2 — Saxony
  DED2: 'Dresden Region',
  DED4: 'Chemnitz Region',
  DED5: 'Leipzig Region',

  // NUTS2 — Saxony-Anhalt
  DEE0: 'Saxony-Anhalt',

  // NUTS2 — Schleswig-Holstein
  DEF0: 'Schleswig-Holstein',

  // NUTS2 — Thuringia
  DEG0: 'Thuringia',

  // NUTS3 — Major cities (kreisfreie Städte)
  DE111: 'Stuttgart',
  DE212: 'Munich',
  DE300: 'Berlin',
  DE501: 'Bremen',
  DE502: 'Bremerhaven',
  DE600: 'Hamburg',
  DE711: 'Darmstadt',
  DE712: 'Frankfurt am Main',
  DE714: 'Wiesbaden',
  DE911: 'Braunschweig',
  DE929: 'Hannover',
  DEA11: 'Düsseldorf',
  DEA12: 'Duisburg',
  DEA13: 'Essen',
  DEA14: 'Krefeld',
  DEA15: 'Mönchengladbach',
  DEA16: 'Mülheim an der Ruhr',
  DEA17: 'Oberhausen',
  DEA18: 'Remscheid',
  DEA19: 'Solingen',
  DEA1A: 'Wuppertal',
  DEA22: 'Bonn',
  DEA23: 'Cologne',
  DEA24: 'Leverkusen',
  DEA31: 'Münster',
  DEA51: 'Bochum',
  DEA52: 'Dortmund',
  DEA53: 'Gelsenkirchen',
  DEA54: 'Hagen',
  DEA55: 'Hamm',
  DEA56: 'Herne',
  DED21: 'Dresden',
  DED51: 'Leipzig',
  DEF01: 'Kiel',
  DEF02: 'Lübeck',
  DEG01: 'Erfurt',
  DEG02: 'Gera',
  DEG03: 'Jena',
  DEG05: 'Weimar',

  // Other EU countries (NUTS1 level for common ones)
  AT:   'Austria',
  AT1:  'Eastern Austria',
  AT2:  'Southern Austria',
  AT3:  'Western Austria',
  BE:   'Belgium',
  CH:   'Switzerland',
  FR:   'France',
  IT:   'Italy',
  NL:   'Netherlands',
  PL:   'Poland',
  ES:   'Spain',
  SE:   'Sweden',
};

/**
 * Returns a human-readable label for a NUTS code.
 * Tries exact match first, then progressively shorter prefixes (NUTS3→NUTS2→NUTS1).
 */
export function nutsLabel(code: string | null | undefined): string {
  if (!code) return 'Unknown';
  const upper = code.toUpperCase().trim();

  // Exact match
  if (NUTS_LABELS[upper]) return NUTS_LABELS[upper];

  // Try progressively shorter prefixes (handles e.g. DE711 → DE71 → DE7)
  for (let len = upper.length - 1; len >= 2; len--) {
    const prefix = upper.substring(0, len);
    if (NUTS_LABELS[prefix]) return `${NUTS_LABELS[prefix]} (${upper})`;
  }

  return upper; // Fall back to the raw code
}

