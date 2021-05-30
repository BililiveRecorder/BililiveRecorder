import * as generators from "./generators"

const argv = process.argv.slice(2)

switch (argv[0]) {
    case "c":
    case "code":
        const availableSections = ["core", "cli", "web", "schema"];
        let sections = argv.slice(1)
        sections = sections.length == 0
            ? availableSections
            : sections.filter(value => {
                const r = availableSections.includes(value);
                if (!r)
                    console.warn(`"${value}" is not a valid section name`)
                return r;
            })
        if (sections.length == 0) {
            console.error("Select valid sections to generate")
        } else {
            generators.code(sections);
        }
        break;
    case "d":
    case "doc":
        if (typeof argv[1] === 'string') {
            generators.doc(argv[1]);
        } else {
            console.error("Missing argument: <path to website> is required")
        }
        break;
    default:
        console.log(`Usage:
    c, code [core, cli, web, schema]
    d, doc <path to website>`);
        break;
}
