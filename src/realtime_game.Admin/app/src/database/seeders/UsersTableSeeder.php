<?php

namespace Database\Seeders;

use App\Models\User;
use Illuminate\Database\Console\Seeds\WithoutModelEvents;
use Illuminate\Database\Seeder;

class UsersTableSeeder extends Seeder
{
    /**
     * Run the database seeds.
     */
    public function run(): void
    {
        User::create([
            'login_id' => 'kaya0215i',
            'password' => '54e7d1d0fc118df5f54cff139f95833ec79d8db3ddf1b93dae0fcad229006cd8',
            'display_name' => 'kaya0215i',
        ]);
        User::create([
            'login_id' => 'kaya0215j',
            'password' => 'c30aeb2700971ef0d27df695c98d788b6d734cda145724f8df60fcaabd7c9fca',
            'display_name' => 'kaya0215j',
        ]);
        User::create([
            'login_id' => 'kaya0215n',
            'password' => '137a69a8d300fb65b5c727050f444f1cf34c668be465fd71e4c4e2900673d2a6',
            'display_name' => 'kaya0215n',
        ]);
    }
}
