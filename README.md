UnityProjectRepositoryのためのTemplate

```
git config --global user.name 'hoge / ほげ'
git config --global user.email 'hoge@example.com'
git config --global core.ignorecase false
git config --global core.autocrlf input
git config --global core.longpaths true

# もしこのがローカル単位で入っていたら殺す
git config --unset core.ignorecase
git config --unset core.autocrlf
git config --unset core.longpaths
```
