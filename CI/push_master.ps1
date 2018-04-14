git tag "v$env:p_version"
git push origin master --quiet
git push origin "v$env:p_version" --quiet
