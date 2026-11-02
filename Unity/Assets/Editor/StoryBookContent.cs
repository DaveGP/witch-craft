using System.Linq;
using Data.ScriptableObjects;
using UnityEditor;
using UnityEngine;
using static WitchCraftEditor.WitchCraftBuildKit;


namespace WitchCraftEditor
{
	// The storybooks hidden around the islands: short tales to find, read, and keep on the bookcase at home.
	public static class StoryBookContent
	{
		private const string Folder = "Assets/Data/Books";

		public static void Build(GameDatabase database)
		{
			database.Books.Clear();

			Add(database, "silver_spellbook", "The Silver Spellbook", "Hazel Thistlewick", new Color(0.78f, 0.82f, 0.9f),
				"On the summit of the tallest southern hill",
				"In the village of Thimble Cross there lived a witch named Marigold who could not remember a single spell.\n\nShe wrote them on the backs of her hands, on the cuffs of her sleeves, and once, in a moment of real desperation, on Pudding, her very patient cat. Pudding walked about for a week with a cure for hiccups across his tummy, and never quite forgave her.",
				"One winter morning Marigold went into the forest for kindling and found a ring of old stumps, white with snow. In the middle of the ring lay a book with a silver cover.\n\nIts pages were empty. But when she held it to her ear, it hummed, the way a kettle hums just before it sings.\n\nShe tucked it under her shawl and carried it home.",
				"That night, as Marigold fumbled for the words to light a candle, something glimmered on the empty page. Letters were appearing, one at a time, written in moonlight.\n\nThe book was remembering the spell for her.\n\nIt waited patiently while she sounded the words out. The candle lit. Pudding, who had been bracing himself to be written on, let out a long breath.",
				"After that, Marigold became rather good at magic.\n\nShe mended Mrs Hobb's teacups, coaxed the sulkiest kettle in the village to boil, and charmed the baker's oven so the bread came out golden every time. People began to knock on her door.\n\nThey knocked when something was broken, and they thanked her very kindly, and then they went home.",
				"And every evening Marigold sat at her table with one bowl, one spoon and one cat, and the cottage felt very large.\n\nSo one night she opened the silver book and asked it for a spell to make friends.\n\nThe page stayed empty.\n\nShe asked the next night, and the night after that. Nothing. Not one silvery letter.",
				"\"Useless thing,\" she said at last, though she didn't really mean it, and she shut the book away in the dresser drawer.\n\nThat was the night the great storm came. Snow piled against the doors, the wind howled down the chimneys, and a little before midnight the mill-keeper's roof gave way under the weight of it all.",
				"Marigold heard the shouting and ran out into the snow. She was halfway down the lane before she realised the silver book was still in the drawer at home.\n\nShe tried to remember a warming spell. A mending spell. Any spell at all. Her head was as empty as the book's pages had been.\n\nSo she did the only thing she could think of. She shouted, \"Everyone to mine!\"",
				"She lit her fire with ordinary matches. She made soup in her biggest pot, and when that ran out, in her second biggest. She found blankets for the mill-keeper's children and a cushion for old Mr Penny's knees.\n\nThere was singing. There was a game of cards that got very serious. Pudding was passed from lap to lap and pretended to hate every minute of it.",
				"In the grey morning, with everyone asleep in heaps around the hearth, Marigold crept to the dresser and opened the drawer.\n\nThe silver book had written a single line, in letters that glowed like embers:\n\n\"There. You knew that one all along.\"",
				"Then, one by one, its pages filled with names. Mrs Hobb. The mill-keeper and his four children. Old Mr Penny. The baker, and the baker's terrible dog.\n\nMarigold never did learn to remember a spell without help.\n\nBut she never once forgot a birthday.");

			Add(database, "eye_of_newt", "Eye of Newt", "Bramble Underleaf", new Color(0.55f, 0.75f, 0.4f),
				"On the western shore, where the grass meets the sand",
				"Every potion book in the land calls for an eye of newt, and every newt in the land is very tired of hearing about it.\n\nNone more so than Nettlebrook, a small spotted newt who lived under a flat stone at the edge of the pond. He was fond of his eyes. He used them for looking at things, and there were a great many things worth looking at.",
				"Each spring the young witches came down to the bank with their baskets, and each spring every newt in the pond hid in the mud until they had gone.\n\n\"It's no way to live,\" said Nettlebrook, the year he turned seven. \"Somebody ought to ask.\"\n\nThe other newts agreed that somebody ought to. Then they all looked at him.",
				"So Nettlebrook set off across the pond to see Granny Sedge, the oldest witch on the island, who lived in a boathouse on the far bank.\n\nIt was a long way for a newt. He crossed on the lily pads, one wobbly hop at a time, while the frogs sunbathing on them made remarks.\n\n\"Off to volunteer, are we?\" croaked the biggest. \"Very brave. Very crunchy.\"",
				"Granny Sedge was shelling peas on her doorstep. Nettlebrook climbed the step, drew himself up as tall as a newt can, and said, \"Madam. Why newts?\"\n\nShe peered at him over her spectacles. \"Why newts what, dear?\"\n\n\"Why our eyes? What have we ever done to anybody?\"",
				"Granny Sedge laughed so hard her hat fell off and floated away down the pond.\n\n\"Oh, you poor dear,\" she wheezed. \"Eye of newt is mustard seed! It's only an old name. Our grandmothers gave every herb in the garden a dreadful name, to keep their recipes secret. Nobody wants your eyes. Nobody has ever wanted your eyes.\"",
				"She fetched a dusty book and showed him the list. Eye of newt: mustard seed. Wool of bat: holly leaves. Tongue of dog: houndstongue, a perfectly nice little flower.\n\nNettlebrook read down the page, then read it again, then pointed with one small hand.\n\n\"And toe of frog?\"\n\n\"Buttercup,\" said Granny Sedge. \"Why?\"\n\n\"No reason,\" said Nettlebrook.",
				"He took the long way home, along the bank, so that he could pass the lily pads very slowly.\n\n\"Lovely afternoon,\" he called up to the biggest frog. \"I've just been reading about potions. Fascinating stuff. Do you happen to know what toe of frog is?\"\n\nThe frog went very pale, for a frog, and did not sunbathe again for a week.",
				"That night the newts threw a party that lasted three days. There was dancing on the stones, singing in the reeds and a great deal of pondweed cake.\n\nThe frogs, who were not invited, sulked loudly from the lily pads the whole time. On the fourth morning Nettlebrook went over and told them about the buttercups, because a joke is only funny until somebody cries.",
				"The frogs were so relieved that they threw a party of their own, and this time everybody came.\n\nTo this day the newts keep a jar of mustard seed by the water's edge, labelled in very careful handwriting: EYE OF NEWT (NOT REALLY). HELP YOURSELF.\n\nThe frogs keep a jar of buttercups beside it. Theirs says: TOE OF FROG. ALSO NOT REALLY. PLEASE.");

			Add(database, "bubble_witch", "Bubble Witch", "Tilly", new Color(0.6f, 0.85f, 0.95f),
				"Atop a sea stack, halfway across to Tidewater",
				"I was a much younger turtle when I knew Pearl, so you will have to trust my memory. Turtles have very good memories. It is one of the few advantages of being slow.\n\nPearl lived on the Tidewater shore with her grandmother, in a cottage made from an upturned boat. She was the smallest witch anyone could remember.",
				"On their ninth birthdays the young witches of Tidewater were given their first brooms. Pearl was given hers, and climbed on, and the broom tipped her gently off into a rock pool.\n\nShe tried again the next day, and the day after. The broom was very sorry about it, but she was simply too small to steer. The crabs began saving her a spot.",
				"So Pearl stopped trying to fly, and started blowing bubbles instead.\n\nShe made her soap from seafoam and kelp and one secret ingredient she never told anyone, not even me. Her bubbles didn't pop when you poked them. They wobbled. They held things.\n\nFirst a feather, then a shell, then a very surprised crab, and then, one breezy afternoon, Pearl herself.",
				"The other young witches laughed at her, though not unkindly. A girl drifting along the beach in a bubble is a funny thing to see.\n\nPearl laughed too. She was having a lovely time. She floated wherever the wind went, and I swam along underneath, and we talked about everything, the way you only can with a friend who is going the same way as you.",
				"Then came the autumn of the great storm.\n\nIt blew in at dusk, black and roaring, and it blew out the lamp on the old lighthouse rock. Out past the reef a fishing boat was coming home. Without the light it was lost among the sea stacks, turning this way and that while the waves grew taller.",
				"The grown witches ran down to the beach with their brooms, but the wind tore at them and threw them back onto the sand. Nobody could fly in such a gale.\n\n\"A broom is a stick,\" said Pearl's grandmother grimly. \"And the wind always wins against a stick.\"\n\nPearl was already unscrewing her soap jar.",
				"She took the deepest breath of her life, and blew.\n\nThe bubble grew bigger than a barrel, bigger than a boat, bigger than her grandmother's cottage. She put her lantern inside it and climbed in after it, and the wind snatched her up and carried her out over the waves.\n\nA bubble doesn't fight the wind, you see. It goes with it.",
				"I swam after her as fast as a young turtle can, which was not nearly fast enough, so I watched it all from the water.\n\nThe bubble glowed gold with lantern light, and silver whenever the lightning flashed. It rolled over the reef and bobbed among the sea stacks, up high where the sailors could see it.\n\nAnd the sailors saw it. And they followed.",
				"The boat came safely up onto the beach just before midnight, and the whole village was there to haul it in.\n\nThen Pearl's bubble drifted down over our heads and popped, and it rained warm sea-spray and laughter on everyone.\n\nThe fishermen said afterwards that it tasted faintly of kelp, and of something else they could never quite name.",
				"Pearl never did learn to ride a broom. She never needed to.\n\nShe is long gone now, and I am an old turtle. But on calm nights, if you look out from Tidewater, you can still see little bubbles rising over the water, each with a tiny light inside.\n\nThe village children blow them for her, to light the way home.");

			Add(database, "phoenix_claw", "The Claw of the Phoenix", "Anonymous", new Color(0.95f, 0.55f, 0.25f),
				"On a hidden isle floating above the southern hills",
				"High above the clouds, on an island that floats where the spire points, there lived a phoenix who had grown tired of burning.\n\nEvery hundred years she flared into flame, crumbled into ash, and hatched again, small and new and remembering nothing at all.\n\n\"It's the forgetting I mind,\" she told the clouds. \"I never get to finish anything.\"",
				"Visitors climbed the spire now and then. They came with nets and spears and polite smiles, and they all wanted the same thing: a claw of the phoenix, which every old book says will brew a potion to make you live forever.\n\nThe phoenix sent them back down rather faster than they had come up, and generally a little singed.",
				"Then one spring a witch climbed up who was not like the others.\n\nShe was old and round and out of breath. On her back she carried a cauldron, a kettle, a picnic blanket and a bag so full of books that the strap had been mended four times.\n\nShe sat down heavily on a rock and said, \"Good afternoon. I've come for a claw of the phoenix.\"",
				"The phoenix ruffled up, ready to scorch. \"For your potion of living forever, I suppose.\"\n\n\"Good heavens, no,\" said the witch. \"Forever sounds exhausting. I want it for a bookmark. I'm a slow reader, and I keep losing my place.\"\n\nThe phoenix laughed so hard she very nearly caught fire eighty years early.",
				"Claws are not for giving away, even to bookish witches. So instead the phoenix plucked a single golden feather from her breast, curved and pointed exactly like a claw, and dropped it into the witch's lap.\n\n\"Thank you,\" said the witch, whose name was Agnes. \"Now. Would you like to hear what happens in chapter one?\"",
				"Agnes put the kettle on, opened the first book, and began to read.\n\nShe came back the next evening, and the next. She read adventures with pirates in them, and love stories where everyone was far too proud to say anything, and one truly terrible cookbook that made them both cry with laughter.\n\nAnd always, when the light went, she marked their place with the golden feather.",
				"Years went by like this, one chapter at a time.\n\nAgnes climbed the spire a little more slowly every spring. Then one autumn she didn't climb it at all. A letter came up instead, carried by a very puzzled pigeon.\n\nInside was the golden feather, tucked into the book they had been reading, about halfway through.",
				"\"My knees have finally won,\" Agnes had written. \"You'll have to finish this one on your own. I've marked our place. Don't you dare skip ahead.\"\n\nThe phoenix did not know how to read. But she had a great deal of time, and she had listened to Agnes for twenty years, and slowly, word by word, she worked it out.",
				"When the hundredth year came, the phoenix burned. She flared up bright as morning, crumbled into ash, and hatched again, small and new.\n\nBut this time, beside her nest, there lay a book with a golden feather in it. She opened it at the marked page, and she remembered. Not everything. But enough.\n\nShe remembered Agnes.",
				"Some say the phoenix still lives on the sky island, reading through the long afternoons, and that she has started writing books of her own.\n\nThis one, you may have noticed, was never signed.\n\nBut if you look closely at its cover, there is a small scorch mark in one corner, shaped exactly like a claw.");

			Add(database, "counted_stars", "The Owl Who Counted Stars", "Hoot", new Color(0.45f, 0.45f, 0.75f),
				"In the Comet Nook past Starfall Ridge, where only a blink will take you",
				"My great-great-grandmother was an owl named Wren, and she was the most precise owl who ever lived.\n\nShe counted everything. The acorns on her oak (two thousand and six). The raindrops on her branch in a shower (too many, but she tried). The number of times her husband said \"Hm\" in an evening (forty, on average; more in winter).",
				"One clear autumn night, Wren looked up and realised that nobody, in the whole history of owls, had ever counted the stars.\n\n\"Well,\" she said. \"That won't do.\"\n\nShe packed a satchel of biscuits, flew to Starfall Ridge, where the stars hang low enough to touch, settled on the highest rock, and began.",
				"One. Two. Three. She went from left to right, the way you read, and from top to bottom, the way you fall asleep.\n\nBy midnight she had reached four thousand and twelve, and she was feeling very pleased with herself.\n\nThen a star sprite drifted right past her beak, giggling, and Wren lost her place entirely.",
				"\"Do you mind?\" said Wren. \"I'm counting.\"\n\n\"Counting what?\" asked the sprite, whose name was Lumen.\n\n\"Stars. You've made me lose my place.\"\n\nLumen looked up at the sky, then back at Wren, and twinkled thoughtfully. \"Did I count as one?\"\n\nWren had to admit she wasn't sure. She sighed, and started again from one.",
				"Night after night the sprites danced by, and night after night Wren lost count.\n\nShe tried counting faster. She tried counting with her eyes half shut. She scratched tallies on the rocks with her talons, but the sprites thought the scratches were a game and rearranged them into pictures of owls.\n\nWren was, for the first time in her life, thoroughly cross.",
				"Then came a night when the clouds rolled in, thick and grey, and there were no stars to count at all.\n\nWren sat on her rock in the dark and felt oddly empty. She waited for the sky to clear. It didn't.\n\nAfter a while a small light bobbed up and settled on the rock beside her. \"No counting tonight?\" said Lumen.",
				"\"No counting tonight,\" said Wren.\n\nSo they talked instead. Lumen told her which stars were the oldest, which ones liked to show off, and which had fallen in love with comets and never been the same since. Wren told Lumen about acorns and raindrops, and her husband's hms.\n\nIt was, she realised as dawn came up, the best night she'd had in years.",
				"When the clouds cleared, Wren went back to the ridge. But she didn't count.\n\nInstead, she asked Lumen the names of things. She learned the stars by heart, the way you learn your friends: not how many, but who.\n\nThe counting had never really been the point, she decided. The sky was. And the company.",
				"Wren lived to be very old and very content. When she was gone, the family found a notebook among her things, labelled in her neat, careful hand: THE STARS. A COMPLETE RECORD.\n\nOn the very first page, it said:\n\n\"Lots. All of them lovely.\"",
				"Nova says it is the most accurate star chart ever made, and Nova would know.\n\nI have started counting the stars too, from my perch on the sky island. Not to finish. Just to keep my great-great-grandmother company.\n\nI am on eleven. I keep getting distracted. They are very lovely.");

			Add(database, "lantern_hill", "The Lantern Under the Hill", "Ember", new Color(1f, 0.7f, 0.35f),
				"In the hollow behind the southernmost hill",
				"Beneath the southern hills there is a cave where the crystals hum. Long ago a fox spirit lived there, all alone.\n\nShe was made of warm light, the way other foxes are made of fur, and her tail flickered like a candle. She had come to the cave in a snowstorm, many lifetimes before, and stayed.",
				"On the night of that storm a traveller had been lost in the hills. The fox spirit had seen his little shape wandering in circles in the snow, and she had called and called, but she was too far away and too faint to be seen.\n\nIn the morning he found his own way home, cold but safe. Still, she never forgot watching him wander, and being no help at all.",
				"So she made a lantern. She chose the clearest crystal in the cave, hollowed it out with her teeth, and breathed a little of her own flame inside.\n\nShe hung it at the mouth of the cave, where it could be seen from every path in the hills.\n\n\"There,\" she said. \"Nobody will ever be lost in the dark again. Not while I'm here.\"",
				"For a while, travellers came. A shepherd in the fog. A pedlar with a broken cartwheel. Two sisters who had been arguing about the way and were both wrong.\n\nThey followed the lantern, sheltered in the cave until morning, and went on their way with a thank you. The fox spirit was glad of them, though none of them ever stayed long.",
				"Then the village built a new road, wide and paved, that went around the hills instead of over them.\n\nYears passed, and nobody came.\n\nThe fox spirit kept the lantern burning anyway. But a lantern lit with your own flame takes a little of you every night, and nobody was giving any back. Her flame grew smaller, and so did she.",
				"In the end she was hardly more than a flicker, curled up beside the lantern, barely able to lift her head.\n\nThen one evening she heard a clatter of hooves, a bleat, and a very determined voice shouting, \"Biscuit! Biscuit, you come back here this minute!\"\n\nA goat trotted into the cave. Close behind it came a small girl with a muddy hem.",
				"The girl caught Biscuit by the collar. Then she saw the lantern, and forgot all about the goat.\n\n\"Oh,\" she said. \"Oh, it's beautiful. Is it yours?\"\n\n\"It is,\" whispered the fox spirit. \"Are you lost?\"\n\nThe girl looked puzzled. \"No. I live just over the hill. I can see the light from my window. I've always wanted to know what it was.\"",
				"Her name was Rosa, and she stayed until the stars came out, asking questions. Did the crystals really hum? (They did.) Did spirits eat supper? (Not usually, but she would try anything once.)\n\nThe next day Rosa came back with bread and honey. The day after, she brought her brother. By the end of the summer, half the village children knew the way to the cave.",
				"With every visitor the fox spirit's flame grew a little brighter, until she glowed so warmly that the whole cave shone gold.\n\nShe had learned something, those long summer evenings. A light isn't only for the lost. It is for whoever is drawn to it.\n\nAnd somebody always is, in the end.",
				"I should tell you: that fox spirit was me.\n\nRosa grew up, and grew old, and her grandchildren still bring me bread and honey on midsummer's eve. My lantern still hangs at the mouth of the cave.\n\nAnd if you are reading this, then you have found me too, which means you followed the light. Come and sit by it a while.");
		}

		private static void Add(GameDatabase database, string id, string title, string author, Color colour, string hint, params string[] pages)
		{
			var book = LoadOrCreate<StoryBookDefinition>($"{Folder}/{id}.asset");

			book.Id = id;
			book.Title = title;
			book.Author = author;
			book.Color = colour;
			book.Hint = hint;
			book.Pages = pages.ToList();

			EditorUtility.SetDirty(book);

			database.Books.Add(book);
		}
	}
}
