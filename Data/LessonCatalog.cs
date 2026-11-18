namespace Syphonic.Data;

/// <summary>
/// The starter course beyond the first three lessons. SeedData adds any lesson from this list
/// whose slug is not in the database yet, so new entries reach existing databases on the next start.
/// To add a lesson: append a LessonSeed with a new slug and a higher Order.
/// </summary>
public static class LessonCatalog
{
    public sealed record LessonSeed(string Slug, string Title, string Summary, int Order, string Content, QuestionSeed[] Questions);

    /// <param name="Options">Two to four answers, in display order.</param>
    /// <param name="Correct">Zero-based index into <paramref name="Options"/>.</param>
    public sealed record QuestionSeed(string Prompt, string[] Options, int Correct, string Explanation);

    public static readonly LessonSeed[] Lessons =
    [
        new("latitude-longitude", "Latitude and longitude",
            "The grid of imaginary lines that gives every place on Earth an address.", 40,
            """
            # Latitude and longitude

            Every place on Earth can be described with two numbers. Together they work like a street address for the whole planet.

            ## Latitude: how far north or south

            Lines of latitude run east to west, like the rungs of a ladder. They measure the distance from the **Equator**, which is 0°.

            - The North Pole is at **90° N** and the South Pole at **90° S**.
            - One degree of latitude is about **111 km** on the ground, wherever you are.

            ## Longitude: how far east or west

            Lines of longitude run from pole to pole. They are measured from the **Prime Meridian** (0°), which passes through Greenwich in London, up to **180°** on the opposite side of the globe.

            Unlike latitude lines, longitude lines are not parallel: they are widest apart at the Equator and meet at the poles.

            ## Five lines worth remembering

            | Line | Latitude |
            |---|---|
            | Arctic Circle | about 66.5° N |
            | Tropic of Cancer | about 23.5° N |
            | Equator | 0° |
            | Tropic of Capricorn | about 23.5° S |
            | Antarctic Circle | about 66.5° S |

            The area between the two tropics is called **the tropics**. It is the only part of Earth where the Sun can be directly overhead.

            ## Reading a coordinate

            Latitude is written first. Cairo is at roughly **30° N, 31° E**: 30 degrees north of the Equator and 31 degrees east of Greenwich.

            > Memory tip: **lat**itude is like the rungs of a **lad**der; **long**itude lines are all the same **long** length from pole to pole.
            """,
            [
                new("What does latitude measure?",
                    ["Distance north or south of the Equator", "Distance east or west of the Prime Meridian", "Height above sea level"], 0,
                    "Latitude runs from 0° at the Equator to 90° at each pole."),
                new("Which line is 0° longitude?",
                    ["The Equator", "The Prime Meridian", "The International Date Line", "The Tropic of Cancer"], 1,
                    "The Prime Meridian passes through Greenwich, London. The Equator is 0° latitude."),
                new("Roughly how far is one degree of latitude on the ground?",
                    ["11 km", "111 km", "1,111 km"], 1,
                    "About 111 km. A handy way to estimate north-south distances from coordinates."),
                new("Where do lines of longitude meet?",
                    ["At the Equator", "At the tropics", "At the poles", "They never meet"], 2,
                    "Longitude lines are widest apart at the Equator and converge at both poles."),
                new("In the coordinate 30° N, 31° E, which number is the latitude?",
                    ["30° N", "31° E"], 0,
                    "Latitude is written first, and it is the one marked north or south."),
            ]),

        new("time-zones", "Time zones",
            "Why it is morning in one country and night in another, and how the world agrees on the time.", 50,
            """
            # Time zones

            Earth spins once every 24 hours, so the Sun is always rising somewhere and setting somewhere else. Time zones keep clock time roughly in step with daylight.

            ## The basic idea

            A full turn is 360°. Divide that by 24 hours and you get **15° of longitude for each hour**. In theory the world has 24 zones, each 15° wide.

            In practice the borders zigzag to follow country and state lines, so that people who live and work together share a clock.

            ## UTC, the world's reference clock

            Times are described as an offset from **UTC** (Coordinated Universal Time), the time at the Prime Meridian.

            - New York is **UTC−5** in winter.
            - Tokyo is **UTC+9**.
            - Places to the **east** are **ahead**: the Sun reaches them first.

            ## The International Date Line

            On the far side of the world from Greenwich, near **180° longitude**, the calendar date changes. Cross it travelling **west** and you skip forward a day; cross it travelling **east** and you repeat a day.

            ## Zones that break the pattern

            - **China** spans about five theoretical zones but uses a single official time.
            - **India** uses UTC+5:30, a half-hour offset. Nepal uses UTC+5:45.
            - **France** has the most time zones of any country, twelve, because of its overseas territories.
            - Many countries move clocks forward in summer (**daylight saving time**); most countries near the Equator do not.
            """,
            [
                new("How many degrees of longitude match one hour of time?",
                    ["10°", "15°", "24°", "30°"], 1,
                    "360° divided by 24 hours is 15° per hour."),
                new("If it is noon UTC, what time is it in a place that uses UTC+9?",
                    ["3:00 in the morning", "9:00 in the morning", "9:00 in the evening"], 2,
                    "UTC+9 is nine hours ahead of UTC, so noon becomes 21:00."),
                new("Near which line of longitude does the International Date Line run?",
                    ["0°", "90° E", "180°"], 2,
                    "It sits on the opposite side of the globe from the Prime Meridian, mostly over open ocean."),
                new("How many official time zones does China use?",
                    ["One", "Three", "Five"], 0,
                    "China is wide enough for about five zones but uses a single national time."),
                new("Places to the east of you are generally...",
                    ["Ahead of your time", "Behind your time", "At the same time"], 0,
                    "Earth turns towards the east, so the Sun reaches eastern places first."),
            ]),

        new("climate-zones", "Climate zones",
            "Why some places are hot, cold, wet or dry, and the few factors that explain most of it.", 60,
            """
            # Climate zones

            **Weather** is what the sky is doing today. **Climate** is the pattern of weather over many years. Knowing a place's climate tells you a lot about its farming, housing and daily life.

            ## Five broad groups

            The most widely used system, the Köppen classification, sorts climates into five main groups:

            1. **Tropical**: hot all year, usually with heavy rain. Found near the Equator.
            2. **Dry**: more water evaporates than falls as rain. Deserts and semi-deserts.
            3. **Temperate**: mild winters and distinct seasons. Much of western Europe and eastern China.
            4. **Continental**: warm summers and very cold winters. Large inland areas such as Siberia and central Canada.
            5. **Polar**: cold all year. The warmest month still averages below 10 °C.

            ## What controls climate

            - **Latitude.** Sunlight strikes the Equator directly and the poles at a low angle, so the tropics receive far more heat.
            - **Altitude.** Air cools by roughly **6.5 °C for every 1,000 m** you climb. That is why Mount Kilimanjaro, almost on the Equator, has ice on top.
            - **Distance from the sea.** Water warms and cools slowly, so coasts have mild seasons while inland areas swing between extremes.
            - **Ocean currents.** The warm Gulf Stream and its extension keep north-western Europe much milder than Canada at the same latitude.
            - **Mountains.** Wet air rises, cools and drops its rain on one side. The far side sits in a dry **rain shadow**.

            ## Try it on the map

            Open the map explorer and compare two cities at a similar latitude, such as London and Calgary. One is by the sea, one is far inland: the climate factors above explain the difference.
            """,
            [
                new("What is the difference between weather and climate?",
                    ["Weather is day to day; climate is the long-term pattern", "Climate is day to day; weather is the long-term pattern", "They mean the same thing"], 0,
                    "Climate is weather averaged over many years."),
                new("Why are the tropics warmer than the poles?",
                    ["They are closer to the Sun", "Sunlight strikes them more directly", "They have more land"], 1,
                    "Near the Equator sunlight arrives almost straight on; near the poles it is spread over a larger area."),
                new("Roughly how much does air cool for every 1,000 m of altitude?",
                    ["About 1 °C", "About 6.5 °C", "About 20 °C"], 1,
                    "About 6.5 °C per 1,000 m, which is why high mountains near the Equator can still carry snow."),
                new("What is a rain shadow?",
                    ["The dry side of a mountain range", "A cloud that blocks the Sun", "The wettest part of a rainforest"], 0,
                    "Air drops its rain as it rises over mountains, leaving the far side dry."),
                new("Which climate group has warm summers and very cold winters, far from the sea?",
                    ["Tropical", "Temperate", "Continental", "Polar"], 2,
                    "Continental climates occur deep inland, where there is no ocean to soften the seasons."),
            ]),

        new("world-biomes", "Biomes of the world",
            "Rainforest, savanna, desert, taiga and tundra: the great natural regions and where to find them.", 70,
            """
            # Biomes of the world

            A **biome** is a large region with similar climate, plants and animals. Climate decides the biome, so the pattern of biomes follows the pattern of climate zones.

            ## From the Equator to the poles

            | Biome | Climate | Where |
            |---|---|---|
            | Tropical rainforest | Hot and wet all year | Amazon basin, Congo basin, Southeast Asia |
            | Savanna | Hot, with a wet and a dry season | East Africa, northern Australia, Brazil |
            | Desert | Very little rain | Sahara, Arabian, Gobi, Atacama |
            | Temperate forest | Mild, four seasons | Western Europe, eastern USA, eastern China |
            | Grassland | Too dry for forest, too wet for desert | Prairies, Pampas, Eurasian steppe |
            | Taiga (boreal forest) | Long cold winters | Canada, Scandinavia, Siberia |
            | Tundra | Frozen most of the year | Arctic coasts |

            ## Facts that surprise people

            - A desert is defined by dryness, not heat: less than about **250 mm of rain a year**. By that measure **Antarctica is the largest desert on Earth**. The Sahara is the largest *hot* desert.
            - The **taiga** is the largest biome on land, a belt of conifer forest around the north of the globe.
            - The **Amazon** is the largest tropical rainforest. Rainforests cover a small share of the land but hold more than half of its plant and animal species.
            - In the tundra the ground below the surface stays frozen all year. This layer is called **permafrost**, and it is why trees cannot grow there.

            ## Altitude copies latitude

            Climbing a high mountain is like travelling towards the pole: forest gives way to grassland, then bare rock, then ice. One tall tropical mountain can pass through several biomes.
            """,
            [
                new("What defines a desert?",
                    ["High temperatures", "Sand dunes", "Very little rain or snow", "No plants at all"], 2,
                    "Deserts receive less than about 250 mm of precipitation a year. They can be hot or cold."),
                new("Which is the largest desert on Earth?",
                    ["Sahara", "Antarctica", "Gobi", "Arabian"], 1,
                    "Antarctica is so dry that it counts as a desert. The Sahara is the largest hot desert."),
                new("Which biome is the largest on land?",
                    ["Tropical rainforest", "Savanna", "Taiga (boreal forest)", "Tundra"], 2,
                    "The taiga circles the north of the globe through Canada, Scandinavia and Siberia."),
                new("What is permafrost?",
                    ["Ground that stays frozen all year", "Snow that falls in summer", "Ice floating on the sea"], 0,
                    "Permafrost lies under the tundra and stops trees from rooting."),
                new("Which biome has a hot climate with one wet season and one dry season?",
                    ["Savanna", "Taiga", "Tundra", "Temperate forest"], 0,
                    "Savannas are tropical grasslands with scattered trees, such as the Serengeti."),
            ]),

        new("mountains-plate-tectonics", "Mountains and moving plates",
            "How slow-moving plates of rock build mountain ranges, open oceans and cause earthquakes.", 80,
            """
            # Mountains and moving plates

            Earth's outer shell is broken into about a dozen large **tectonic plates**. They move a few centimetres a year, about as fast as fingernails grow. Most mountains, volcanoes and earthquakes happen where plates meet.

            ## Three kinds of plate boundary

            - **Colliding (convergent).** Two plates push together. When both carry continents, the crust crumples upward into mountains. When an ocean plate meets a continent, the ocean plate sinks beneath it, forming a deep trench and a line of volcanoes.
            - **Separating (divergent).** Plates pull apart and molten rock rises to fill the gap, creating new crust. The **Mid-Atlantic Ridge** is widening the Atlantic by a few centimetres a year.
            - **Sliding (transform).** Plates grind past each other. The **San Andreas Fault** in California is the best-known example.

            ## Ranges to know

            - **Himalayas**: formed as India collides with Asia. They include **Mount Everest, 8,849 m**, the highest point above sea level, and they are still rising.
            - **Andes**: the longest mountain range on land, about **7,000 km** down the west side of South America.
            - **Rockies**, **Alps** and **Atlas**: the main ranges of North America, Europe and north-west Africa.

            ## The Ring of Fire

            A horseshoe of plate boundaries surrounds the Pacific Ocean. About three quarters of the world's active volcanoes and most of its large earthquakes occur along it, from Chile through Alaska and Japan to New Zealand.

            ## The deepest place

            The **Mariana Trench** in the western Pacific reaches almost **11,000 m** below sea level, deeper than Everest is tall. It marks where one plate dives beneath another.
            """,
            [
                new("How fast do tectonic plates move?",
                    ["A few centimetres a year", "A few metres a year", "A few kilometres a year"], 0,
                    "Roughly the speed at which fingernails grow."),
                new("Which collision formed the Himalayas?",
                    ["Africa and Europe", "India and Asia", "North and South America"], 1,
                    "The Indian plate is still pushing into Asia, so the range is still rising."),
                new("Which is the longest mountain range on land?",
                    ["Himalayas", "Rockies", "Andes", "Alps"], 2,
                    "The Andes run about 7,000 km along the west of South America."),
                new("Around which ocean is the Ring of Fire?",
                    ["Atlantic", "Indian", "Pacific", "Arctic"], 2,
                    "Most of the world's volcanoes and large earthquakes ring the Pacific."),
                new("What happens at the Mid-Atlantic Ridge?",
                    ["Plates pull apart and new crust forms", "Plates collide and build mountains", "One plate slides sideways past another"], 0,
                    "It is a divergent boundary that is slowly widening the Atlantic Ocean."),
            ]),

        new("rivers-lakes", "Rivers and lakes",
            "How rivers work from source to sea, and the record-holding rivers and lakes worth knowing.", 90,
            """
            # Rivers and lakes

            Rivers carry rain and meltwater from high ground to the sea. Most of the world's great cities grew beside one.

            ## The parts of a river

            - **Source**: where the river begins, often a spring, lake or glacier in high ground.
            - **Tributary**: a smaller river that flows into a larger one.
            - **Drainage basin**: all the land whose water drains into one river. The edge between two basins is a **watershed**.
            - **Meander**: a bend in a slow river crossing flat land.
            - **Mouth**: where the river meets the sea or a lake.
            - **Delta**: a fan of land built from mud the river drops at its mouth. The Nile and Ganges deltas are among the most densely farmed places on Earth.

            ## Rivers to know

            | River | Continent | Known for |
            |---|---|---|
            | Nile | Africa | Traditionally the longest, about 6,650 km |
            | Amazon | South America | Carries by far the most water |
            | Yangtze | Asia | Longest river in Asia |
            | Mississippi–Missouri | North America | Drains much of the United States |
            | Danube | Europe | Flows through ten countries |

            The Nile and the Amazon are close in length, and the answer depends on where you say each one starts. For volume there is no contest: the Amazon carries about a fifth of all river water reaching the oceans.

            ## Lakes to know

            - **Caspian Sea**: the largest lake by area, despite its name. Its water is salty.
            - **Lake Superior**: the largest freshwater lake by surface area.
            - **Lake Baikal** in Siberia: the deepest lake, more than 1,600 m, holding about a fifth of the world's unfrozen fresh water.
            - **Lake Victoria**: the largest lake in Africa and a source of the Nile.
            """,
            [
                new("What is a tributary?",
                    ["A river that flows into a larger river", "The place where a river begins", "Land built up at a river's mouth"], 0,
                    "Tributaries add their water to a main river. The Missouri is a tributary of the Mississippi."),
                new("Which river carries the most water?",
                    ["Nile", "Amazon", "Yangtze", "Danube"], 1,
                    "The Amazon carries about a fifth of all river water that reaches the oceans."),
                new("What is a delta?",
                    ["A bend in a river", "Land built from mud dropped at a river's mouth", "The area a river drains"], 1,
                    "Rivers slow down at the sea and drop the mud they carry, building a fan of new land."),
                new("Which is the deepest lake in the world?",
                    ["Lake Superior", "Lake Victoria", "Lake Baikal", "Caspian Sea"], 2,
                    "Lake Baikal in Siberia is more than 1,600 m deep."),
                new("The Caspian Sea is usually classed as...",
                    ["The largest lake by area", "The smallest ocean", "A river delta"], 0,
                    "It is landlocked, so geographers treat it as the world's largest lake."),
            ]),

        new("population-cities", "Population and cities",
            "How many people there are, where they live, and why more than half now live in cities.", 100,
            """
            # Population and cities

            The world's population passed **8 billion** in November 2022. It took until about 1800 to reach the first billion and only eleven years to go from seven to eight.

            ## Where people live

            People are spread very unevenly.

            - **Asia** is home to about **six in every ten** people.
            - **India** and **China** each have more than 1.4 billion. India became the most populous country in 2023.
            - Large areas are almost empty: deserts, high mountains, the far north and Antarctica.

            People cluster where farming is easy and water is close: river valleys, coasts and fertile plains.

            ## Density, not just totals

            **Population density** is the number of people per square kilometre. It tells you how crowded a place is.

            - Bangladesh has more than 1,100 people per km².
            - Mongolia and Australia have about 2 to 3 per km².

            Use "Colour countries by → Population density" in the map explorer to see the pattern.

            ## A world of cities

            Since about 2007, **more than half** of all people have lived in towns and cities. The share keeps rising as people move for work, schools and services. This shift is called **urbanisation**.

            - A **megacity** has more than **10 million** people.
            - **Tokyo** is the largest urban area, with around 37 million people.
            - Most of the fastest-growing cities today are in Africa and South Asia.

            ## Growth is slowing

            Families are smaller than they were fifty years ago in almost every country, so the population is still rising but more slowly each decade. Some countries, including Japan and Italy, are already shrinking.
            """,
            [
                new("When did the world population pass 8 billion?",
                    ["1999", "2011", "2022"], 2,
                    "The United Nations marked 8 billion in November 2022."),
                new("Roughly what share of people live in Asia?",
                    ["About 3 in 10", "About 6 in 10", "About 9 in 10"], 1,
                    "Asia holds about 60% of the world's population."),
                new("What does population density measure?",
                    ["The total number of people in a country", "People per square kilometre", "How fast a population is growing"], 1,
                    "Density shows how crowded a place is, whatever its total population."),
                new("How many people does a megacity have?",
                    ["More than 1 million", "More than 10 million", "More than 100 million"], 1,
                    "Tokyo, Delhi, Shanghai and São Paulo are all megacities."),
                new("What is urbanisation?",
                    ["A growing share of people living in towns and cities", "People moving from cities to farms", "The building of roads between countries"], 0,
                    "More than half of all people have lived in urban areas since about 2007."),
            ]),

        new("countries-capitals-borders", "Countries, capitals and borders",
            "What counts as a country, why capitals are not always the biggest city, and border terms to know.", 110,
            """
            # Countries, capitals and borders

            The political map changes more often than the physical one. Knowing how it is organised makes news and history easier to follow.

            ## How many countries?

            The United Nations has **193 member states**. The total is sometimes given as 195 or more, because a few territories are recognised by some governments and not by others.

            - **Russia** is the largest country by area, spanning eleven time zones.
            - **Vatican City**, inside Rome, is the smallest: under half a square kilometre.

            ## Capitals

            A capital is where the national government sits. It is often the largest city, but not always.

            | Country | Capital | Largest city |
            |---|---|---|
            | Australia | Canberra | Sydney |
            | Canada | Ottawa | Toronto |
            | Brazil | Brasília | São Paulo |
            | Türkiye | Ankara | Istanbul |
            | United States | Washington, D.C. | New York |

            Several of these capitals were purpose-built or chosen as a compromise between rival cities. A few countries split the job: South Africa has three capitals.

            ## Border words

            - **Landlocked**: a country with no coast, such as Bolivia, Switzerland or Mongolia. There are 44 of them.
            - **Doubly landlocked**: surrounded only by landlocked countries. There are just two, **Liechtenstein** and **Uzbekistan**.
            - **Enclave**: a country completely surrounded by one other. **Lesotho** sits inside South Africa; San Marino and Vatican City sit inside Italy.
            - **Island country**: no land borders at all, such as Japan, Iceland and Madagascar.

            China and Russia have the most neighbours, with fourteen land borders each.

            ## Practise it

            Country facts stick through repetition. Use **Practice → Country drills** to test capitals and continents, and the map explorer's challenge to find countries by shape.
            """,
            [
                new("How many member states does the United Nations have?",
                    ["93", "193", "293"], 1,
                    "There are 193 UN member states."),
                new("What is the capital of Australia?",
                    ["Sydney", "Melbourne", "Canberra", "Perth"], 2,
                    "Canberra was purpose-built as a compromise between Sydney and Melbourne."),
                new("What does landlocked mean?",
                    ["Having no coastline", "Being an island", "Having only one neighbour"], 0,
                    "Landlocked countries, such as Bolivia and Mongolia, have no direct access to the sea."),
                new("Which country is an enclave inside South Africa?",
                    ["Lesotho", "Kenya", "Uruguay", "Nepal"], 0,
                    "Lesotho is completely surrounded by South Africa."),
                new("Which is the largest country by area?",
                    ["Canada", "China", "United States", "Russia"], 3,
                    "Russia covers about 17 million km², spanning eleven time zones."),
            ]),
    ];
}
