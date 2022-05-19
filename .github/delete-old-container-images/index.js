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

const packageInfo = await octokit.request('GET /orgs/{org}/packages/{package_type}/{package_name}', {
    ...common,
});

const page_count = Math.ceil(packageInfo.data.version_count / 100);

console.log(`共有 ${page_count} 页, ${packageInfo.data.version_count} 个版本`);

const now = new Date();

for (let page = page_count; page > 0; page--) {
    const versions = await octokit.request('GET /orgs/{org}/packages/{package_type}/{package_name}/versions', {
        ...common,
        per_page: 100,
        page: page
    })

    const toBeDeleted = versions.data
        .filter(x => (x.metadata?.container?.tags || []).length == 0)
        .filter(x => !x.deleted_at)
        .filter(x => (now - new Date(x.created_at)) > (1000 * 60 * 60 * 24 * 21)); // 21 天

    console.log(`第 ${page} 页要删除 ${toBeDeleted.length} / ${versions.data.length} 个版本`);

    for (const version of toBeDeleted) {

        console.log("    删除: " + version.id);

        await octokit.request('DELETE /orgs/{org}/packages/{package_type}/{package_name}/versions/{package_version_id}', {
            ...common,
            package_version_id: version.id
        })
    }
}
