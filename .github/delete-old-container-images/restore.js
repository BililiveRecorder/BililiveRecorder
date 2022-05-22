import { env } from 'process';
import { Octokit } from "@octokit/core";

// Octokit.js
// https://github.com/octokit/core.js#readme
const octokit = new Octokit({
    auth: env.GITHUB_TOKEN
})

const common = {
    package_type: 'container',
    package_name: 'bililiverecorder',
    org: env.GITHUB_REPOSITORY_OWNER,
}


const ids = [


];

let inProgress = [];

const restoredIds = [];

function printAndEnd() {
    console.log("Caught interrupt signal");
    console.log('================ ids:');
    console.log(JSON.stringify(ids));
    console.log('================ inProgress:');
    console.log(JSON.stringify(inProgress));
    console.log('================ restoredIds:');
    console.log(JSON.stringify(restoredIds));
    process.exit();
}

process.on('SIGINT', printAndEnd);

async function start() {
    while (ids.length > 0) {
        const id = ids.pop();
        inProgress.push(id);

        await doRestore(id);

        inProgress = inProgress.filter(x => x != id);
        restoredIds.push(id);
    }
}

async function doRestore(id) {
    console.log(`R>: ${id}  -  ${new Date()}`);
    const resp = await octokit.request('POST /orgs/{org}/packages/{package_type}/{package_name}/versions/{package_version_id}/restore', {
        ...common,
        package_version_id: id
    });
    console.log(`OK: ${id}  -  ${new Date()}`);
}
try {
    await Promise.all([start(), start(), start(), start(), start(), start()]);
} catch (error) {
    console.error(error)
    printAndEnd()
}

printAndEnd()
